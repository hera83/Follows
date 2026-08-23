using System.Collections.Concurrent;

namespace web.Infrastructure
{
    public enum DocumentTranslationJobStatus { Running, Completed, Failed }

    /// <summary>
    /// In-memory state for one in-flight or finished document translation job. See
    /// <see cref="DocumentTranslationJobTracker"/> for why this exists.
    /// </summary>
    public sealed class DocumentTranslationJobState
    {
        /// <summary>Owning user's id — TranslateStatus checks this so one user can't poll another's job id.</summary>
        public required string UserId { get; init; }

        public DocumentTranslationJobStatus Status { get; set; } = DocumentTranslationJobStatus.Running;

        /// <summary>-1 until the document's text has been extracted and split into chunks (see DocumentsService.TranslateDocumentAsync).</summary>
        public int TotalChunks { get; set; } = -1;
        public int CompletedChunks { get; set; }

        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool AlreadyInTargetLanguage { get; set; }
        public string TargetLanguageName { get; set; } = string.Empty;
        public string? Html { get; set; }
        public bool Truncated { get; set; }

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Holds in-memory progress for document translations running in a detached background task, so the
    /// "Oversæt"-button can poll "X af Y" instead of holding one HTTP request open for the whole
    /// translation (which is what used to trip proxy/gateway timeouts on long documents — see
    /// DocumentsController.TranslateStart/TranslateStatus). Chunks are still translated strictly one at a
    /// time server-side, same as before — this only changes how progress is reported to the browser, not
    /// how the AI Gateway is called.
    ///
    /// Purely in-process bookkeeping: a job doesn't survive an app restart, and a client polling a job that
    /// vanished (restart, or TTL prune below) just gets a 404 and shows a "prøv igen" toast — the actual
    /// translation work is unaffected either way, since a finished translation is already cached in
    /// DocumentTranslations by the time anyone would notice.
    /// </summary>
    public sealed class DocumentTranslationJobTracker
    {
        // Belt-and-braces cleanup for jobs whose client never polls again (closed tab, navigated away) -
        // TranslateStatus already removes a job as soon as it's been read once it's Completed/Failed, so
        // this only ever catches abandoned ones.
        private static readonly TimeSpan JobTtl = TimeSpan.FromMinutes(30);

        private readonly ConcurrentDictionary<string, DocumentTranslationJobState> _jobs = new();

        public string Start(string userId)
        {
            var jobId = Guid.NewGuid().ToString("N");
            _jobs[jobId] = new DocumentTranslationJobState { UserId = userId };
            return jobId;
        }

        public void ReportChunkCount(string jobId, int totalChunks)
        {
            if (_jobs.TryGetValue(jobId, out var job)) job.TotalChunks = totalChunks;
        }

        public void ReportProgress(string jobId, int completedChunks)
        {
            if (_jobs.TryGetValue(jobId, out var job)) job.CompletedChunks = completedChunks;
        }

        public void Complete(string jobId, Action<DocumentTranslationJobState> apply)
        {
            if (!_jobs.TryGetValue(jobId, out var job)) return;
            apply(job);
            job.Status = DocumentTranslationJobStatus.Completed;
        }

        public void Fail(string jobId, string errorMessage)
        {
            if (!_jobs.TryGetValue(jobId, out var job)) return;
            job.ErrorMessage = errorMessage;
            job.Status = DocumentTranslationJobStatus.Failed;
        }

        /// <summary>Returns the job for <paramref name="userId"/>, or null if it doesn't exist, has expired, or belongs to someone else.</summary>
        public DocumentTranslationJobState? Get(string jobId, string userId)
        {
            PruneExpired();
            return _jobs.TryGetValue(jobId, out var job) && job.UserId == userId ? job : null;
        }

        public void Remove(string jobId) => _jobs.TryRemove(jobId, out _);

        private void PruneExpired()
        {
            var cutoff = DateTime.UtcNow - JobTtl;
            foreach (var (id, job) in _jobs)
            {
                if (job.CreatedAtUtc < cutoff) _jobs.TryRemove(id, out _);
            }
        }
    }
}

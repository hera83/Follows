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
        public required int DocumentId { get; init; }

        /// <summary>Owning user's id — TranslateStatus checks this so one user can't poll another's job id.</summary>
        public required string UserId { get; init; }

        public DocumentTranslationJobStatus Status { get; set; } = DocumentTranslationJobStatus.Running;

        /// <summary>True until the job has acquired TranslationSlot and actually started work — see DocumentTranslationJobTracker.</summary>
        public bool Queued { get; set; } = true;

        /// <summary>-1 until the document's text has been extracted and split into chunks (see DocumentsService.TranslateDocumentAsync).</summary>
        public int TotalChunks { get; set; } = -1;
        public int CompletedChunks { get; set; }

        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool AlreadyInTargetLanguage { get; set; }
        public string TargetLanguageName { get; set; } = string.Empty;
        public string? Html { get; set; }
        public bool Truncated { get; set; }

        /// <summary>Info/warning toast text for AlreadyInTargetLanguage or Truncated, already translated to the viewer's profile language — see TranslateDocumentResponseDto.Message for why this is built server-side.</summary>
        public string? Message { get; set; }

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Holds in-memory progress for document translations running in a detached background task, so the
    /// "Oversæt"-button can poll "X af Y" instead of holding one HTTP request open for the whole
    /// translation (which is what used to trip proxy/gateway timeouts on long documents — see
    /// DocumentsController.TranslateStart/TranslateStatus).
    ///
    /// Also the single place that enforces "one document translation call to the AI Gateway at a time" —
    /// <see cref="TranslationSlot"/> is a 1-slot semaphore the background task must acquire before calling
    /// DocumentsService, and <see cref="TryStart"/> refuses to spin up a second background task for a
    /// document that already has one running. Both exist because a stuck/slow job used to be able to sit
    /// there forever (nothing ever cancelled it - see JobTimeout on the controller side) while a second
    /// click queued a second job behind it, competing for the same single-request-at-a-time AI Gateway and
    /// showing as "0 af N" not moving for many minutes even though nothing was actually broken.
    ///
    /// Purely in-process bookkeeping: a job doesn't survive an app restart, and a client polling a job that
    /// vanished (restart, or TTL prune below) just gets a 404 and shows a "prøv igen" toast — the actual
    /// translation work is unaffected either way, since a finished translation is already cached in
    /// DocumentTranslations by the time anyone would notice.
    /// </summary>
    public sealed class DocumentTranslationJobTracker
    {
        /// <summary>
        /// Held by the background task for the whole duration of one document translation (see
        /// DocumentsController.TranslateStart) - guarantees only one document-translation call to the AI
        /// Gateway is ever in flight, since it can't serve more than one at a time.
        /// </summary>
        public SemaphoreSlim TranslationSlot { get; } = new(1, 1);

        // Belt-and-braces cleanup for jobs whose client never polls again (closed tab, navigated away) -
        // TranslateStatus already removes a job as soon as it's been read once it's Completed/Failed, so
        // this only ever catches abandoned ones.
        private static readonly TimeSpan JobTtl = TimeSpan.FromMinutes(30);

        private readonly ConcurrentDictionary<string, DocumentTranslationJobState> _jobs = new();

        // documentId -> jobId, for the one currently running/queued job for that document, if any.
        private readonly ConcurrentDictionary<int, string> _activeJobByDocument = new();

        /// <summary>
        /// Starts a new job for <paramref name="documentId"/>, or hands back the id of one already running
        /// for it (<paramref name="isNew"/> false) so the caller can skip spinning up a second background
        /// task that would just queue behind the first one for no reason.
        /// </summary>
        public string TryStart(string userId, int documentId, out bool isNew)
        {
            PruneExpired();

            if (_activeJobByDocument.TryGetValue(documentId, out var existingJobId) && _jobs.ContainsKey(existingJobId))
            {
                isNew = false;
                return existingJobId;
            }

            var jobId = Guid.NewGuid().ToString("N");
            _jobs[jobId] = new DocumentTranslationJobState { DocumentId = documentId, UserId = userId };
            _activeJobByDocument[documentId] = jobId;
            isNew = true;
            return jobId;
        }

        public void MarkStarted(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job)) job.Queued = false;
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
            _activeJobByDocument.TryRemove(new KeyValuePair<int, string>(job.DocumentId, jobId));
        }

        public void Fail(string jobId, string errorMessage)
        {
            if (!_jobs.TryGetValue(jobId, out var job)) return;
            job.ErrorMessage = errorMessage;
            job.Status = DocumentTranslationJobStatus.Failed;
            _activeJobByDocument.TryRemove(new KeyValuePair<int, string>(job.DocumentId, jobId));
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
                if (job.CreatedAtUtc < cutoff)
                {
                    _jobs.TryRemove(id, out _);
                    _activeJobByDocument.TryRemove(new KeyValuePair<int, string>(job.DocumentId, id));
                }
            }
        }
    }
}

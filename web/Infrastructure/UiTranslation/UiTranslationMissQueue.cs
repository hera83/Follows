using System.Collections.Concurrent;

namespace web.Infrastructure.UiTranslation
{
    /// <summary>
    /// Singleton, in-process inbox of "@T()/@TJs() was called with a Danish string that isn't in the
    /// preloaded catalog yet" events — see UiTranslationCatalogService.T. Never blocks the request that
    /// hit the miss (it always gets the Danish text back immediately); UiTranslationBackgroundWorker
    /// drains this periodically to (a) register brand-new Danish source strings into the "da" catalog
    /// registry and (b) top up any language that had a real miss, via UiTranslationBulkService. This is
    /// what makes the catalog self-populating — no manual list of every UI string to maintain anywhere.
    ///
    /// Purely in-process bookkeeping, same as DocumentTranslationJobTracker/UiLocalizationJobTracker — a
    /// miss that's still queued when the app restarts is simply re-queued the next time that string is
    /// rendered, no data is lost in a way that matters (the Danish fallback keeps working regardless).
    /// </summary>
    public sealed class UiTranslationMissQueue
    {
        private readonly ConcurrentDictionary<(string LanguageCode, string Hash), string> _misses = new();

        public void Enqueue(string languageCode, string hash, string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText)) return;
            _misses.TryAdd((languageCode, hash), sourceText);
        }

        /// <summary>Atomically empties the queue and returns everything that was in it.</summary>
        public List<KeyValuePair<(string LanguageCode, string Hash), string>> DrainAll()
        {
            var snapshot = _misses.ToList();
            foreach (var kv in snapshot)
                _misses.TryRemove(kv.Key, out _);
            return snapshot;
        }

        /// <summary>Distinct language codes currently sitting in the queue, without draining it — lets a caller (UiTranslationBackgroundWorker) decide what to act on before something else's drain empties it.</summary>
        public IReadOnlyCollection<string> Peek() => _misses.Keys.Select(k => k.LanguageCode).Distinct().ToList();
    }
}

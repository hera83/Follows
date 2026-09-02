using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Services.VideoTranscoding.Dtos;
using web.Services.VideoTranscoding.Interfaces;

namespace web.BgSerives
{
    /// <summary>
    /// Re-encodes newly uploaded feed videos to H.264/AAC in the background via
    /// <see cref="IVideoTranscodingService"/>. Uploaded video is frequently HEVC (the default on
    /// iPhone), which browsers can only decode through a hardware path that's unreliable across
    /// devices — see VideoTranscodingService for the full reasoning. Picks up MediaType == Video rows
    /// with TranscodeStatus null (never triaged — covers rows from before this feature existed),
    /// Pending, or stuck on Processing from a run that never finished (e.g. the app restarted mid-job).
    /// </summary>
    public class VideoTranscodeWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VideoTranscodeWorker> _logger;
        private readonly TimeSpan _interval;

        public VideoTranscodeWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<VideoTranscodeWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var intervalSeconds = configuration.GetValue<int?>("VideoTranscode:WorkerIntervalSeconds") ?? 15;
            _interval = TimeSpan.FromSeconds(intervalSeconds > 0 ? intervalSeconds : 15);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (true)
            {
                try
                {
                    await TranscodePendingVideosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "VideoTranscodeWorker iteration failed");
                }

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task TranscodePendingVideosAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var transcodingService = scope.ServiceProvider.GetRequiredService<IVideoTranscodingService>();

            var pending = await dbContext.FeedMedia
                .Include(m => m.File)
                .Where(m => m.MediaType == FeedMediaType.Video
                    && (m.TranscodeStatus == null
                        || m.TranscodeStatus == VideoTranscodeStatus.Pending
                        || m.TranscodeStatus == VideoTranscodeStatus.Processing))
                .OrderBy(m => m.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0) return;

            foreach (var media in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (media.File is null)
                {
                    media.TranscodeStatus = VideoTranscodeStatus.Failed;
                    continue;
                }

                media.TranscodeStatus = VideoTranscodeStatus.Processing;
                await dbContext.SaveChangesAsync(cancellationToken);

                var inputPath = Path.Combine(env.ContentRootPath, media.File.StoredPath);
                if (!File.Exists(inputPath))
                {
                    _logger.LogWarning("FeedMedia {MediaId} points at a missing file: {InputPath}", media.Id, inputPath);
                    media.TranscodeStatus = VideoTranscodeStatus.Failed;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var outputFileName = $"{Guid.NewGuid()}.mp4";
                var outputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, outputFileName);

                TranscodeVideoResponseDto result;
                try
                {
                    result = await transcodingService.TranscodeToH264Async(
                        new TranscodeVideoRequestDto { InputPath = inputPath, OutputPath = outputPath },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transcoding threw for FeedMedia {MediaId} ({InputPath})", media.Id, inputPath);
                    result = new TranscodeVideoResponseDto { Success = false, ErrorMessage = ex.Message };
                }

                if (result.Success)
                {
                    var oldInputPath = inputPath;

                    media.File.StoredFileName = outputFileName;
                    media.File.StoredPath = Path.GetRelativePath(env.ContentRootPath, outputPath);
                    media.File.ContentType = "video/mp4";
                    media.File.FileSizeBytes = result.OutputFileSizeBytes;
                    media.File.UpdatedAtUtc = DateTime.UtcNow;
                    media.TranscodeStatus = VideoTranscodeStatus.Completed;

                    try
                    {
                        if (File.Exists(oldInputPath)) File.Delete(oldInputPath);
                    }
                    catch (Exception ex)
                    {
                        // Not fatal — the new file is already what gets served from now on, this just
                        // leaves the original HEVC upload orphaned on disk instead of cleaned up.
                        _logger.LogWarning(ex, "Could not delete original file after transcoding FeedMedia {MediaId}: {Path}", media.Id, oldInputPath);
                    }

                    _logger.LogInformation("Transcoded FeedMedia {MediaId} to H.264 ({SizeBytes} bytes)", media.Id, result.OutputFileSizeBytes);
                }
                else
                {
                    // Leave the original file in place — still playable via a native app, just not
                    // guaranteed to be smooth in-browser. Better than losing it.
                    media.TranscodeStatus = VideoTranscodeStatus.Failed;
                    _logger.LogError("Failed to transcode FeedMedia {MediaId}: {Error}", media.Id, result.ErrorMessage);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

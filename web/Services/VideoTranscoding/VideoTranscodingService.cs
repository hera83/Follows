using System.Diagnostics;
using System.Text;
using web.Services.VideoTranscoding.Dtos;
using web.Services.VideoTranscoding.Interfaces;

namespace web.Services.VideoTranscoding
{
    /// <summary>
    /// Shells out to the ffmpeg CLI to re-encode a video to H.264/AAC MP4 — the one combination every
    /// browser can reliably decode (in software, not just via a hardware path). Uploaded phone video is
    /// very often HEVC (the default on iPhone), which Chromium browsers can only play via hardware
    /// decoding; that path is known to be unstable across devices (stutter, dropped frames, playback
    /// dying mid-stream) even though the exact same file is fine in a standalone player with a proper
    /// software HEVC decoder. Re-encoding removes the dependency on that hardware path entirely.
    /// </summary>
    public class VideoTranscodingService : IVideoTranscodingService
    {
        private readonly string _ffmpegPath;
        private readonly TimeSpan _timeout;
        private readonly ILogger<VideoTranscodingService> _logger;

        public VideoTranscodingService(IConfiguration configuration, ILogger<VideoTranscodingService> logger)
        {
            _logger = logger;
            _ffmpegPath = configuration["VideoTranscode:FfmpegPath"] is { Length: > 0 } configuredPath ? configuredPath : "ffmpeg";

            var timeoutMinutes = configuration.GetValue<int?>("VideoTranscode:TimeoutMinutes") ?? 20;
            _timeout = TimeSpan.FromMinutes(timeoutMinutes > 0 ? timeoutMinutes : 20);
        }

        public async Task<TranscodeVideoResponseDto> TranscodeToH264Async(TranscodeVideoRequestDto request, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(_timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // ArgumentList (not a concatenated Arguments string) so paths containing spaces — very
            // common on Windows ("...\Heine Ramskov\...") — don't need manual quoting.
            foreach (var arg in BuildArguments(request.InputPath, request.OutputPath))
                startInfo.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = startInfo };
            var stderr = new StringBuilder();
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not start ffmpeg at '{FfmpegPath}' — is it installed and on PATH, or is VideoTranscode:FfmpegPath set correctly?", _ffmpegPath);
                return new TranscodeVideoResponseDto { Success = false, ErrorMessage = $"ffmpeg kunne ikke startes ('{_ffmpegPath}'). Er det installeret?" };
            }

            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                var reason = cancellationToken.IsCancellationRequested ? "afbrudt" : $"tog mere end {_timeout.TotalMinutes:0} minutter";
                return new TranscodeVideoResponseDto { Success = false, ErrorMessage = $"Omkodning blev {reason}." };
            }

            if (process.ExitCode != 0 || !File.Exists(request.OutputPath))
            {
                _logger.LogWarning("ffmpeg exited {ExitCode} for {InputPath}: {Stderr}", process.ExitCode, request.InputPath, stderr.ToString());
                TryDelete(request.OutputPath);
                return new TranscodeVideoResponseDto { Success = false, ErrorMessage = $"ffmpeg fejlede (kode {process.ExitCode})." };
            }

            var outputSize = new FileInfo(request.OutputPath).Length;
            if (outputSize == 0)
            {
                TryDelete(request.OutputPath);
                return new TranscodeVideoResponseDto { Success = false, ErrorMessage = "ffmpeg producerede en tom fil." };
            }

            return new TranscodeVideoResponseDto { Success = true, OutputFileSizeBytes = outputSize };
        }

        private static IEnumerable<string> BuildArguments(string inputPath, string outputPath)
        {
            yield return "-y"; // overwrite outputPath without prompting
            yield return "-i";
            yield return inputPath;
            yield return "-c:v";
            yield return "libx264";
            yield return "-preset";
            yield return "veryfast";
            yield return "-crf";
            yield return "23";
            // Cap resolution at 1080p — 4K phone footage re-encoded at full size is still needlessly
            // heavy to decode; -2 keeps the height even (required by libx264) while preserving aspect.
            yield return "-vf";
            yield return "scale='min(1920,iw)':-2";
            // Broadest pixel-format compatibility — some HEVC sources are 10-bit/4:2:2, which not
            // every H.264 decoder handles.
            yield return "-pix_fmt";
            yield return "yuv420p";
            yield return "-c:a";
            yield return "aac";
            yield return "-b:a";
            yield return "128k";
            // Moves the moov atom to the front so the file is playable/seekable as soon as the first
            // bytes arrive, instead of needing the whole file downloaded first.
            yield return "-movflags";
            yield return "+faststart";
            yield return outputPath;
        }

        private void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill timed-out ffmpeg process");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup of a failed/partial output file — nothing more to do if it's locked.
            }
        }
    }
}

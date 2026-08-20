using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace web.Services.AiGateway;

/// <summary>
/// Wrapper omkring en åben WebSocket-forbindelse til AiGatewayens /Speaches/TranscribeRealtime.
/// AiGateway taler samme JSON-events-protokol som OpenAIs Realtime API: alle beskeder - både ind
/// og ud - er tekst-frames med et JSON-objekt der har et "type"-felt (fx "session.created",
/// "session.update", "input_audio_buffer.append",
/// "conversation.item.input_audio_transcription.completed"). Lyd sendes IKKE som rå binære
/// WebSocket-frames, men base64-kodet PCM16-lyd inde i "input_audio_buffer.append"-events - se
/// <see cref="AppendAudioAsync"/>. Denne wrapper (de-)serialiserer bevidst ikke det fulde
/// event-skema (det har mange event-typer og er ikke beskrevet i AiGatewayens OpenAPI-dokument),
/// men lader kaldere sende/parse de rå JSON-tekstbeskeder efter behov.
/// </summary>
public sealed class AiGatewayRealtimeTranscriptionSession : IAsyncDisposable
{
    private readonly ClientWebSocket _socket;

    internal AiGatewayRealtimeTranscriptionSession(ClientWebSocket socket)
    {
        _socket = socket;
    }

    public WebSocketState State => _socket.State;

    /// <summary>Sender et rått JSON-event som tekst-frame, fx "session.update" eller "response.create".</summary>
    public Task SendEventAsync(object @event, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(@event);
        var bytes = Encoding.UTF8.GetBytes(json);
        return _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    /// <summary>Tilføjer en PCM16 (16-bit, little-endian) lydchunk til input-bufferen via "input_audio_buffer.append".</summary>
    public Task AppendAudioAsync(ReadOnlyMemory<byte> pcm16Audio, CancellationToken cancellationToken = default)
        => SendEventAsync(new { type = "input_audio_buffer.append", audio = Convert.ToBase64String(pcm16Audio.Span) }, cancellationToken);

    /// <summary>Committer input-bufferen ("input_audio_buffer.commit"), så serveren transskriberer det sendte lydsegment.</summary>
    public Task CommitAudioAsync(CancellationToken cancellationToken = default)
        => SendEventAsync(new { type = "input_audio_buffer.commit" }, cancellationToken);

    public Task CloseAsync(CancellationToken cancellationToken = default)
        => _socket.State == WebSocketState.Open
            ? _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancellationToken)
            : Task.CompletedTask;

    /// <summary>Modtager de rå JSON-tekst-events fra serveren løbende, ét pr. besked (fx "session.created", "conversation.item.input_audio_transcription.completed").</summary>
    public async IAsyncEnumerable<string> ReceiveEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new byte[8192];
        using var messageStream = new MemoryStream();

        while (_socket.State == WebSocketState.Open)
        {
            messageStream.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    yield break;

                messageStream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                yield return Encoding.UTF8.GetString(messageStream.ToArray());
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "disposing", CancellationToken.None);
            }
            catch
            {
                // Best effort - forbindelsen lukkes uanset hvad.
            }
        }

        _socket.Dispose();
    }
}

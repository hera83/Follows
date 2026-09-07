namespace web.Services.AiGateway;

public class AiGatewaySettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string? ApiKey { get; set; }
    public string? DefaultChatModel { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? DefaultTtsModel { get; set; }
    public string? DefaultTtsVoice { get; set; }
    public string? DefaultSttModel { get; set; }

    /// <summary>
    /// Chat model used specifically for translation — Documents' "Oversæt"-button (see
    /// DocumentsService.TranslateDocumentAsync) and Feed's post/comment auto-translate (see
    /// FeedService.TryTranslateAsync/TryDetectLanguageCodeAsync) — overriding DefaultChatModel for those
    /// features only. Falls back to DefaultChatModel when unset.
    ///
    /// Exists because DefaultChatModel is sometimes a reasoning ("thinking") model, which turned out to be
    /// unreliable and slow for translation specifically: live testing against this same AI Gateway
    /// (gemma4:12b as DefaultChatModel at the time) showed it would silently spend its entire token budget
    /// on hidden reasoning and return an empty answer - confirmed with NumPredict=-1 (unlimited): 7,351
    /// tokens generated, 0 of them ever reaching the answer, for a translation that should take a few
    /// hundred tokens. Swapping in a plain instruction-following model (qwen2.5:14b-instruct, already
    /// available on the same gateway) for translation calls resolved it completely - same chunk, 63 seconds
    /// instead of 250+, correct complete translation, no retries needed. Set this to any non-reasoning chat
    /// model your AI Gateway has available; leave empty to just use DefaultChatModel (fine if that's
    /// already a non-reasoning model).
    /// </summary>
    public string? TranslationModel { get; set; }

    /// <summary>
    /// Vision-capable chat model used for the Documents OCR fallback (see
    /// DocumentsService.TranslateDocumentAsync / LanguageTools.RecognizeImageTextAsync) — triggered when a
    /// PDF's page has no extractable text at all (page.Letters is empty; see
    /// DocumentMarkdownExtractor.ExtractPdf's comment on "born vector" PDFs — text drawn as outline paths
    /// instead of real text runs, e.g. from certain "print to PDF" pipelines — where the underlying
    /// characters genuinely don't exist in the file, so no text-extraction library can recover them, only
    /// reading the rendered pixels can). Left empty, the OCR fallback is skipped entirely and such PDFs
    /// keep failing with the ordinary "no text found" error — DefaultChatModel/TranslationModel are
    /// ordinary text models and can't be assumed to understand image input, so this needs its own explicit
    /// setting rather than falling back to either of them.
    /// </summary>
    public string? VisionModel { get; set; }

    public int RequestTimeoutSeconds { get; set; } = 300;
}

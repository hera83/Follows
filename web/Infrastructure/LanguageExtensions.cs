using System.Net;
using System.Text.RegularExpressions;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Interfaces;

namespace web.Infrastructure
{
    // Indgangspunkt: saml AiGateway-klienten og dens konfiguration i ét LanguageTools-objekt,
    // typisk én gang i en controllers konstruktør, hvor begge allerede er injiceret via DI:
    //
    //   _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
    //
    // Alle sprogfunktioner kaldes derefter direkte på _language uden at skulle sende
    // configurationProvider med hver gang - se LanguageTools nedenfor.
    public static class LanguageExtensions
    {
        public static LanguageTools Language(this IAiGatewayService aiGateway, IAiGatewayConfigurationProvider configurationProvider)
            => new(aiGateway, configurationProvider);
    }

    /// <summary>
    /// Sprog-værktøjer bygget oven på AiGatewayens Ollama-chat-endpoint: oversættelse, sprog-detektion,
    /// opsummering m.m. Opret via <see cref="LanguageExtensions.Language"/>. Alle metoder sender én
    /// system+user-besked af sted med lav temperatur, så svaret bliver kort og deterministisk, og
    /// bruger den konfigurerede DefaultChatModel medmindre <c>model</c> angives eksplicit.
    /// </summary>
    public sealed class LanguageTools
    {
        private readonly IAiGatewayService _aiGateway;
        private readonly IAiGatewayConfigurationProvider _configurationProvider;

        internal LanguageTools(IAiGatewayService aiGateway, IAiGatewayConfigurationProvider configurationProvider)
        {
            _aiGateway = aiGateway;
            _configurationProvider = configurationProvider;
        }

        /// <summary>
        /// Oversætter <paramref name="text"/> til <paramref name="targetLanguage"/> (fx "engelsk", "tysk").
        /// Angiv <paramref name="sourceLanguage"/> hvis kildesproget allerede kendes.
        /// </summary>
        public Task<string> TranslateAsync(
            string text,
            string targetLanguage,
            string? sourceLanguage = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            var systemPrompt = string.IsNullOrWhiteSpace(sourceLanguage)
                ? $"Du er en oversætter. Oversæt teksten brugeren sender til {targetLanguage}. " +
                  "Svar udelukkende med den oversatte tekst - ingen forklaringer, ingen anførselstegn, ingen ekstra kommentarer."
                : $"Du er en oversætter. Oversæt teksten brugeren sender fra {sourceLanguage} til {targetLanguage}. " +
                  "Svar udelukkende med den oversatte tekst - ingen forklaringer, ingen anførselstegn, ingen ekstra kommentarer.";

            return CompleteAsync(systemPrompt, text, model, cancellationToken);
        }

        /// <summary>
        /// Oversætter <paramref name="content"/> (plain text pulled from a document — a PDF, Word/Excel/
        /// PowerPoint file, etc.) til <paramref name="targetLanguage"/> og formaterer resultatet som
        /// Markdown undervejs: overskrifter, lister og tabeller (GFM pipe-tabeller) bevares/genskabes hvis
        /// kildeteksten indeholder dem, så visningen bliver læsbar frem for én lang klump tekst.
        /// </summary>
        public Task<string> TranslateDocumentToMarkdownAsync(
            string content,
            string targetLanguage,
            string? sourceLanguage = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Task.FromResult(string.Empty);

            var sourceClause = string.IsNullOrWhiteSpace(sourceLanguage) ? string.Empty : $" fra {sourceLanguage}";
            var systemPrompt =
                $"Du er en oversætter og formatterer. Oversæt teksten brugeren sender{sourceClause} til {targetLanguage}, " +
                "og formater resultatet som pænt Markdown. Teksten stammer fra et dokument og kan indeholde løs " +
                "struktur fra den oprindelige side- eller celleopdeling - genskab den som Markdown: overskrifter med #, " +
                "punktlister eller nummererede lister, og tabeldata som Markdown-tabeller (pipe-syntaks med skillelinje). " +
                "Bevar al indhold og rækkefølge. Svar direkte med selve den oversatte Markdown-tekst med det samme - " +
                "ræsonnér ikke trin for trin og forklar ikke dine overvejelser først. Svar udelukkende med den " +
                "oversatte Markdown-tekst - ingen indledning, ingen forklaringer, ingen kodeblok-indpakning (```) om " +
                "hele svaret.";

            // Dokument-tekst er typisk meget længere end en kort oversættelse/opsummering. Ollamas
            // standard num_ctx er ofte kun 2048 tokens medmindre modellens Modelfile sætter noget
            // andet - rigeligt til en sætning, men ikke til et helt dokument-chunk plus dets oversatte
            // Markdown-output. Uden dette sætter generering simpelthen bare i stå midt i svaret, uden
            // fejl, når konteksten løber tør (det er derfor DocumentsService også deler lange dokumenter
            // op i chunks - de to ting er hinandens sikkerhedsnet, ikke alternativer).
            //
            // "Tænkende" modeller (se CleanResponse-kommentaren nedenfor om gemma4:12b) kan bruge hele
            // budgettet på et <think>-ræsonnement og aldrig nå frem til selve svaret, hvilket - efter
            // CleanResponse har fjernet <think>-blokken - ser ud som et tomt svar. AiGatewayen har ingen
            // "think: false"-mulighed at sende videre til Ollama (se dens swagger: ChatRequestDto har
            // ingen think-felt og additionalProperties: false), så der er ingen API-vej til at slå det
            // fra herfra - kun den eksplicitte instruks ovenfor i systemPrompt om at svare direkte.
            //
            // 4096 (ikke modellens fulde 32k-vindue) - sat lavt med vilje. KV-cachen Ollama skal allokere
            // til selve kontekstvinduet vokser lineært med num_ctx, oveni modelvægtene selv (~7-9 GB for en
            // kvantiseret 12B-model) - på et 16 GB V100-kort levnede 16384 for lidt VRAM til at holde
            // stabilt loaded, hvilket i praksis viste sig som skiftevis "connection refused" (Ollama crasher/
            // genstarter på OOM) og flere-minutter-lange hæng (CPU-offload når GPU'en løber tør). Et enkelt
            // chunk her er DocumentLimits.TranslationChunkChars (~900 tegn, ~250-300 tokens) plus system-
            // prompt og det oversatte Markdown-svar - 4096 giver rigelig margin til det uden at presse VRAM.
            var options = new OllamaOptionsDto { Temperature = 0.2, NumCtx = 4096, NumPredict = -1 };

            return CompleteAsync(systemPrompt, content, model, options, cancellationToken);
        }

        /// <summary>
        /// Genkender hvilket sprog <paramref name="text"/> er skrevet på og returnerer sprognavnet på dansk
        /// (fx "Dansk", "Engelsk").
        /// </summary>
        public async Task<string> DetectLanguageAsync(
            string text,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            const string systemPrompt =
                "Du genkender sprog. Læs teksten brugeren sender, og svar udelukkende med navnet på sproget - " +
                "på dansk, med stort forbogstav (fx \"Dansk\", \"Engelsk\", \"Tysk\"). Svar ikke med andet end sprognavnet.";

            var result = await CompleteAsync(systemPrompt, text, model, cancellationToken);
            return result.TrimEnd('.', ' ');
        }

        /// <summary>
        /// Same as <see cref="DetectLanguageAsync"/>, but maps the answer straight to a
        /// <see cref="web.Constants.AppLanguages"/> code (e.g. "en") instead of the raw Danish name.
        /// Returns null if detection came back empty or didn't match a known language name.
        /// </summary>
        public async Task<string?> DetectLanguageCodeAsync(
            string text,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            var detectedName = await DetectLanguageAsync(text, model, cancellationToken);
            return web.Constants.AppLanguages.CodeFromDanishName(detectedName);
        }

        /// <summary>
        /// Vurderer om <paramref name="text"/> er skrevet på <paramref name="expectedLanguage"/>.
        /// </summary>
        public async Task<bool> IsLanguageAsync(
            string text,
            string expectedLanguage,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var systemPrompt =
                $"Du vurderer sprog. Læs teksten brugeren sender, og svar udelukkende med \"ja\" hvis den er skrevet på " +
                $"{expectedLanguage}, ellers \"nej\". Svar ikke med andet.";

            var result = await CompleteAsync(systemPrompt, text, model, cancellationToken);
            return result.TrimStart().StartsWith("ja", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Opsummerer <paramref name="text"/> i højst <paramref name="maxSentences"/> sætninger.
        /// Opsummeres på samme sprog som teksten, medmindre <paramref name="language"/> angives.
        /// </summary>
        public Task<string> SummarizeAsync(
            string text,
            int maxSentences = 3,
            string? language = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            var languageInstruction = string.IsNullOrWhiteSpace(language) ? "på samme sprog som teksten" : $"på {language}";
            var systemPrompt =
                $"Du opsummerer tekst. Opsummer teksten brugeren sender i højst {maxSentences} sætninger, {languageInstruction}. " +
                "Svar udelukkende med opsummeringen - ingen indledning, ingen forklaringer.";

            return CompleteAsync(systemPrompt, text, model, cancellationToken);
        }

        /// <summary>
        /// Retter stave- og grammatikfejl i <paramref name="text"/> uden at ændre betydning, tone eller formatering.
        /// </summary>
        public Task<string> CorrectSpellingAsync(
            string text,
            string? language = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            var languageInstruction = string.IsNullOrWhiteSpace(language) ? string.Empty : $" Teksten er på {language}.";
            var systemPrompt =
                "Du retter stave- og grammatikfejl." + languageInstruction +
                " Ret fejlene i teksten brugeren sender, men bevar betydning, tone og formatering. " +
                "Svar udelukkende med den rettede tekst - ingen forklaringer.";

            return CompleteAsync(systemPrompt, text, model, cancellationToken);
        }

        /// <summary>
        /// Omskriver <paramref name="text"/> til letforståeligt sprog (korte sætninger, enkle ord) uden at ændre betydningen.
        /// </summary>
        public Task<string> SimplifyLanguageAsync(
            string text,
            string? language = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            var languageInstruction = string.IsNullOrWhiteSpace(language) ? "på samme sprog som teksten" : $"på {language}";
            var systemPrompt =
                $"Du omskriver tekst til letforståeligt sprog, {languageInstruction}. Brug korte sætninger og enkle ord, " +
                "men bevar den oprindelige betydning. Svar udelukkende med den omskrevne tekst - ingen forklaringer.";

            return CompleteAsync(systemPrompt, text, model, cancellationToken);
        }

        /// <summary>
        /// Omskriver <paramref name="text"/> så den fremstår med en anden tone (fx "formel", "uformel", "venlig").
        /// </summary>
        public Task<string> ChangeToneAsync(
            string text,
            string tone,
            string? language = null,
            string? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            var languageInstruction = string.IsNullOrWhiteSpace(language) ? string.Empty : $" Skriv på {language}.";
            var systemPrompt =
                $"Du omskriver tekst, så den fremstår {tone}.{languageInstruction} Bevar den oprindelige betydning og alle " +
                "centrale informationer. Svar udelukkende med den omskrevne tekst - ingen forklaringer.";

            return CompleteAsync(systemPrompt, text, model, cancellationToken);
        }

        // Nogle modeller (set bl.a. med gemma4:12b) lækker rå styre-tokens fra deres chat-template
        // ind i svaret, fx "<channel|>" eller "<|message|>" foran selve teksten, eller hele
        // ræsonnement-blokke i "<think>...</think>" (kendt fra reasoning-modeller som DeepSeek-R1/QwQ).
        // Det ødelægger både visning og simple ja/nej-tjek som IsLanguageAsync, så det luges væk her,
        // ét sted, i stedet for i hver enkelt funktion.
        private static readonly Regex ThinkBlockRegex = new(@"<think>[\s\S]*?</think>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ControlTokenRegex = new(@"<\|[a-zA-Z_][a-zA-Z0-9_]*\|?>|<[a-zA-Z_][a-zA-Z0-9_]*\|>", RegexOptions.Compiled);

        private static string CleanResponse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = ThinkBlockRegex.Replace(raw, string.Empty);
            text = ControlTokenRegex.Replace(text, string.Empty);
            return text.Trim();
        }

        // Fælles kald mod AiGatewayens Ollama-chat: slår DefaultChatModel op hvis intet model-navn er
        // angivet, sender system+user-besked med lav temperatur, og rydder op i svaret. Samme
        // model-opløsningsmønster som ChatController bruger mod chat-UI'en.
        private Task<string> CompleteAsync(string systemPrompt, string userPrompt, string? model, CancellationToken cancellationToken)
            => CompleteAsync(systemPrompt, userPrompt, model, options: null, cancellationToken);

        // Overload der lader kaldere override Ollama-options (fx num_ctx/num_predict for lange
        // dokument-oversættelser, se TranslateDocumentToMarkdownAsync) - falder tilbage til den
        // simple lav-temperatur-opsætning, når intet er angivet.
        private async Task<string> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            string? model,
            OllamaOptionsDto? options,
            CancellationToken cancellationToken)
        {
            var resolvedModel = model;
            if (string.IsNullOrWhiteSpace(resolvedModel))
            {
                var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
                resolvedModel = config.DefaultChatModel;
            }

            if (string.IsNullOrWhiteSpace(resolvedModel))
            {
                throw new AiGatewayException(
                    HttpStatusCode.BadRequest,
                    "Ingen model valgt, og der er ikke sat en standardmodel i AiGateway-indstillingerne.");
            }

            var response = await _aiGateway.OllamaChatAsync(new ChatRequestDto
            {
                Model = resolvedModel,
                Messages = new List<OllamaMessageDto>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
                },
                Options = options ?? new OllamaOptionsDto { Temperature = 0.2 }
            }, cancellationToken);

            return CleanResponse(response.Message?.Content);
        }
    }
}

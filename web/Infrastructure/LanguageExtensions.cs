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
        private async Task<string> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            string? model,
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
                Options = new OllamaOptionsDto { Temperature = 0.2 }
            }, cancellationToken);

            return CleanResponse(response.Message?.Content);
        }
    }
}

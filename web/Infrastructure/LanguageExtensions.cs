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

            // Holdt kort og direkte med vilje - en bruger-test af samme model/AI-gateway viste at et langt,
            // fler-klausulet systemprompt (den forrige version her havde 5-6 separate instruktioner bundtet
            // sammen) reelt ser ud til at give en "tænkende" model mere at overveje, før den svarer -
            // hvilket øger risikoen for tomme/afbrudte chunks (se DocumentsService.TranslateDocumentAsync's
            // retry-logik). Kortere og mere ligetil klarede sig markant bedre i praksis.
            var sourceClause = string.IsNullOrWhiteSpace(sourceLanguage) ? string.Empty : $" fra {sourceLanguage}";
            var systemPrompt =
                $"Du er oversætter. Oversæt teksten brugeren sender{sourceClause} til {targetLanguage}, og behold " +
                "Markdown-struktur (overskrifter med #, lister med -, tabeller med pipe-syntaks) hvis kildeteksten " +
                "har den slags struktur. Bevar alt indhold og rækkefølgen. " +
                "Returner kun den oversatte tekst - ingen indledning, forklaring, spørgsmål eller kodeblok-indpakning.";

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
            // Afvejning mellem to modsatrettede problemer, begge set i praksis på et 16 GB V100-kort med
            // gemma4:12b:
            //  - For højt (16384): KV-cachen bliver for stor til at holde modellen stabilt loaded -
            //    viste sig som skiftevis "connection refused" (Ollama crasher/genstarter på OOM) og
            //    flere-minutter-lange hæng (CPU-offload når GPU'en løber tør).
            //  - For lavt (4096): modellen (som ræsonnerer skjult i <think>...</think> før den svarer,
            //    se CleanResponse nedenfor) bruger det meste af det lille budget på selve ræsonnementet og
            //    når aldrig frem til et faktisk svar - endte som tomme chunks (se DocumentsService's log
            //    "returned an empty chunk twice ... giving up") selvom kaldet i sig selv lykkedes fint.
            // 8192 er et forsøg på en mellemvej - dobbelt hovedrum ift. 4096 til ræsonnement + svar, stadig
            // langt fra 16384's VRAM-forbrug. Juster videre i den ene eller anden retning ud fra hvad der
            // reelt sker på serveren (VRAM-forbrug vs. tomme chunks).
            //
            // NumPredict er sat til et loft i stedet for -1 (ubegrænset) - uden det kan et enkelt forsøg i
            // værste fald bruge hele NumCtx-budgettet på skjult ræsonnement, uden nogensinde at ramme en
            // fejl eller returnere noget, hvilket gør ét mislykket forsøg unødigt langsomt. Med et loft
            // stopper generering tidligere, så et forsøg der er ved at gå galt, fejler hurtigere og kan nå
            // at blive retried (se DocumentsService's retry-loop) inden for den samlede job-timeout.
            var options = new OllamaOptionsDto { Temperature = 0.2, NumCtx = 8192, NumPredict = 3000 };

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

            // The explicit "aldrig ud fra emnet..." clause and worked example below aren't decorative -
            // confirmed by live testing that the plain, shorter version of this prompt (just "answer with
            // the language name") makes the model confuse a text's TOPIC with its actual language: an
            // English text that happens to mention a Danish company/city name (e.g. "Welcome to Nordvang
            // A/S in Denmark") was consistently (6/6 in testing) misidentified as "Dansk" - it was
            // pattern-matching the subject matter, not reading the actual words. The same short prompt
            // correctly identified other languages (French, German) that didn't share this specific
            // confusion, which is what makes this a real bug and not just "the model is bad at this" -
            // adding one concrete example of exactly this trap fixed it consistently (6/6 in testing,
            // including re-checking it didn't regress French/German/Danish detection).
            const string systemPrompt =
                "Du genkender hvilket sprog en tekst er SKREVET PÅ, ud fra dens ord, stavning og grammatik - " +
                "aldrig ud fra emnet, stednavne eller firmanavne i teksten. Eksempel: teksten \"Welcome to " +
                "Nordvang A/S in Denmark\" handler om et dansk firma, men er skrevet på engelsk (ord som " +
                "\"Welcome\", \"to\", \"in\" er engelske) - svaret er derfor Engelsk, ikke Dansk. Svar " +
                "udelukkende med navnet på sproget - på dansk, med stort forbogstav (Dansk, Engelsk, Tysk, " +
                "Fransk, osv). Svar ikke med andet end selve sprognavnet, uanset hvilket sprog teksten er " +
                "skrevet på.";

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
                Options = options ?? new OllamaOptionsDto { Temperature = 0.2 },
                // Uden dette bruger Ollama sin egen standard keep_alive (typisk 5 min) og læsser modellen af
                // hukommelsen derefter - næste kald (fx det følgende chunk i en dokument-oversættelse, eller
                // et Feed-oversæt-kald der interleaver) betaler så en fuld genindlæsning oveni selve
                // genereringen, hvilket for en model i denne størrelse nemt kan koste 30-60+ sekunder pr.
                // gang. Sat generøst højt her, fælles for alle LanguageTools-kald, så modellen typisk
                // forbliver loaded gennem en hel dokument-oversættelse (flere kald i træk) og videre ind i
                // almindelig Feed-brug, i stedet for konstant at blive smidt ud og genindlæst.
                KeepAlive = "30m"
            }, cancellationToken);

            return CleanResponse(response.Message?.Content);
        }
    }
}

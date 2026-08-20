namespace web.Constants
{
    /// <summary>
    /// Fixed interface labels for the Feeds translation UI ("Oversat" / "Oversætter..."), translated
    /// into every web.Constants.AppLanguages language. These are static and shown instantly — unlike
    /// post/comment content, they're not worth a live AI translation call (or a cache row) just to
    /// display three short, unchanging words.
    /// </summary>
    public sealed record FeedTranslationLabels(string Translated, string Translating, string TranslatingBannerFormat);

    public static class FeedTranslationStrings
    {
        private static readonly Dictionary<string, FeedTranslationLabels> Labels = new()
        {
            ["da"] = new("Oversat", "Oversætter...", "Oversætter {0} opslag..."),
            ["en"] = new("Translated", "Translating...", "Translating {0} posts..."),
            ["zh"] = new("已翻译", "正在翻译...", "正在翻译 {0} 条帖子..."),
            ["hi"] = new("अनुवादित", "अनुवाद हो रहा है...", "{0} पोस्ट का अनुवाद हो रहा है..."),
            ["es"] = new("Traducido", "Traduciendo...", "Traduciendo {0} publicaciones..."),
            ["fr"] = new("Traduit", "Traduction en cours...", "Traduction de {0} publications en cours..."),
            ["ar"] = new("مترجم", "جارٍ الترجمة...", "جارٍ ترجمة {0} منشورات..."),
            ["bn"] = new("অনূদিত", "অনুবাদ করা হচ্ছে...", "{0}টি পোস্ট অনুবাদ করা হচ্ছে..."),
            ["pt"] = new("Traduzido", "Traduzindo...", "Traduzindo {0} publicações..."),
            ["ru"] = new("Переведено", "Перевод...", "Перевод {0} публикаций..."),
            ["ur"] = new("ترجمہ شدہ", "ترجمہ ہو رہا ہے...", "{0} پوسٹس کا ترجمہ ہو رہا ہے..."),
            ["id"] = new("Diterjemahkan", "Menerjemahkan...", "Menerjemahkan {0} postingan..."),
            ["de"] = new("Übersetzt", "Wird übersetzt...", "{0} Beiträge werden übersetzt..."),
            ["ja"] = new("翻訳済み", "翻訳中...", "{0}件の投稿を翻訳中..."),
            ["mr"] = new("भाषांतरित", "भाषांतर करत आहे...", "{0} पोस्ट भाषांतरित करत आहे..."),
            ["te"] = new("అనువదించబడింది", "అనువదిస్తోంది...", "{0} పోస్ట్‌లను అనువదిస్తోంది..."),
            ["tr"] = new("Çevrildi", "Çevriliyor...", "{0} gönderi çevriliyor..."),
            ["ta"] = new("மொழிபெயர்க்கப்பட்டது", "மொழிபெயர்க்கப்படுகிறது...", "{0} இடுகைகள் மொழிபெயர்க்கப்படுகின்றன..."),
            ["vi"] = new("Đã dịch", "Đang dịch...", "Đang dịch {0} bài viết..."),
            ["ko"] = new("번역됨", "번역 중...", "게시물 {0}개 번역 중..."),
            ["it"] = new("Tradotto", "Traduzione in corso...", "Traduzione di {0} post in corso..."),
            ["th"] = new("แปลแล้ว", "กำลังแปล...", "กำลังแปล {0} โพสต์..."),
            ["fa"] = new("ترجمه‌شده", "در حال ترجمه...", "در حال ترجمه {0} پست..."),
            ["sw"] = new("Imetafsiriwa", "Inatafsiri...", "Inatafsiri machapisho {0}..."),
            ["pl"] = new("Przetłumaczone", "Tłumaczenie...", "Tłumaczenie {0} postów..."),
            ["uk"] = new("Перекладено", "Переклад...", "Переклад {0} дописів..."),
            ["nl"] = new("Vertaald", "Vertalen...", "{0} berichten worden vertaald..."),
            ["el"] = new("Μεταφρασμένο", "Μετάφραση...", "Μετάφραση {0} αναρτήσεων..."),
            ["sv"] = new("Översatt", "Översätter...", "Översätter {0} inlägg..."),
            ["no"] = new("Oversatt", "Oversetter...", "Oversetter {0} innlegg..."),
            ["fi"] = new("Käännetty", "Käännetään...", "Käännetään {0} julkaisua..."),
            ["is"] = new("Þýtt", "Þýðir...", "Þýðir {0} færslur..."),
        };

        /// <summary>Labels for a language code, falling back to Dansk (AppLanguages.Default) if unrecognized.</summary>
        public static FeedTranslationLabels GetLabels(string? languageCode)
        {
            var code = AppLanguages.Normalize(languageCode);
            return Labels.TryGetValue(code, out var labels) ? labels : Labels[AppLanguages.Default];
        }
    }
}

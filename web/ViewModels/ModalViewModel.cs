namespace web.ViewModels
{
    public class ModalViewModel
    {
        public string Id { get; set; } = "app-modal";
        public string Title { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public string? FooterHtml { get; set; }
        public string DialogCssClass { get; set; } = string.Empty;
    }
}

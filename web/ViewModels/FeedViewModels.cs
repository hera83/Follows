using System.ComponentModel.DataAnnotations;
using web.Constants;

namespace web.ViewModels
{
    /// <summary>Model for Feed/Index — the composer plus the first page of posts.</summary>
    public class FeedIndexViewModel
    {
        public List<FeedPostViewModel> Posts { get; set; } = new();
        public bool HasMore { get; set; }
        public int? NextBeforeId { get; set; }

        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserDisplayName { get; set; } = string.Empty;
        public bool CurrentUserHasAvatar { get; set; }

        /// <summary>"Oversætter {0} opslag..." (or the viewer's own language) for the top progress banner.</summary>
        public string TranslatingBannerFormat { get; set; } = string.Empty;

        /// <summary>
        /// "Oversætter..." — in the viewer's own language. Same label as FeedPostViewModel.TranslatingLabel,
        /// exposed here too so the client can render the per-post "is translating" badge on a post that
        /// just picked up a new comment, without waiting on a full post re-render to get the text.
        /// </summary>
        public string TranslatingLabel { get; set; } = string.Empty;
    }

    /// <summary>One story in the feed, with its media, like state and comments.</summary>
    public class FeedPostViewModel
    {
        public int Id { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public bool AuthorHasAvatar { get; set; }
        public string? Caption { get; set; }

        /// <summary>True when Caption is shown machine-translated to the viewer's preferred language.</summary>
        public bool IsTranslated { get; set; }

        /// <summary>
        /// True when this post (its caption or a comment) still needs translating and is shown in its
        /// original language for now — the client fetches the translation in the background.
        /// </summary>
        public bool NeedsTranslation { get; set; }

        /// <summary>"Oversat" — in the viewer's own language. Shown on the caption when IsTranslated.</summary>
        public string TranslatedLabel { get; set; } = string.Empty;

        /// <summary>"Oversætter..." — in the viewer's own language. Shown on the post when NeedsTranslation.</summary>
        public string TranslatingLabel { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public List<FeedMediaViewModel> Media { get; set; } = new();
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<FeedCommentViewModel> Comments { get; set; } = new();

        /// <summary>True when the current user is the author, or an admin/developer.</summary>
        public bool CanDelete { get; set; }
    }

    public class FeedMediaViewModel
    {
        public int Id { get; set; }
        public bool IsVideo { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class FeedCommentViewModel
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorDisplayName { get; set; } = string.Empty;
        public bool AuthorHasAvatar { get; set; }
        public string Body { get; set; } = string.Empty;

        /// <summary>True when Body is shown machine-translated to the viewer's preferred language.</summary>
        public bool IsTranslated { get; set; }

        /// <summary>"Oversat" — in the viewer's own language. Shown on the comment when IsTranslated.</summary>
        public string TranslatedLabel { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>True when the current user is the author, or an admin/developer.</summary>
        public bool CanDelete { get; set; }
    }

    /// <summary>Bound from the composer form (multipart/form-data).</summary>
    public class CreateFeedPostViewModel
    {
        [MaxLength(FeedLimits.MaxCaptionLength, ErrorMessage = "Historien må højst være 4000 tegn.")]
        public string? Caption { get; set; }

        public List<IFormFile>? Files { get; set; }
    }

    public class AddFeedCommentViewModel
    {
        [Required]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Skriv en kommentar.")]
        [MaxLength(FeedLimits.MaxCommentLength, ErrorMessage = "Kommentaren må højst være 1000 tegn.")]
        public string Body { get; set; } = string.Empty;
    }
}

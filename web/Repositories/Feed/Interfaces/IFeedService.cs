using web.Repositories.Feed.Dtos;

namespace web.Repositories.Feed.Interfaces
{
    public interface IFeedService
    {
        Task<GetFeedPageResponseDto> GetFeedPageAsync(GetFeedPageRequestDto dto, CancellationToken ct = default);
        Task<FeedPostDto?> GetPostAsync(int postId, string currentUserId, string viewerLanguage, CancellationToken ct = default);

        /// <summary>
        /// Same shape as GetPostAsync but never calls the translation service — cached translations are
        /// used, anything else is returned as-is with NeedsTranslation flagged. Used right after the
        /// current user creates a post, since it's guaranteed to already be in their own language: no
        /// reason to make them wait on a live translate/verify pass just to see their own new post appear.
        /// </summary>
        Task<FeedPostDto?> GetPostFastAsync(int postId, string currentUserId, string viewerLanguage, CancellationToken ct = default);

        Task<CreateFeedPostResponseDto> CreatePostAsync(CreateFeedPostRequestDto dto, CancellationToken ct = default);
        Task<DeleteFeedPostResponseDto> DeletePostAsync(int postId, string requestingUserId, bool isModerator, CancellationToken ct = default);

        /// <summary>Fetches a post's original (untranslated) caption and date for the edit modal, after checking the caller may edit it.</summary>
        Task<GetFeedPostForEditResponseDto> GetPostForEditAsync(int postId, string requestingUserId, bool isModerator, CancellationToken ct = default);

        /// <summary>Updates a post's caption, and — Administrator/Developer only — its CreatedAtUtc, so old events can be backdated to their real date.</summary>
        Task<EditFeedPostResponseDto> EditPostAsync(EditFeedPostRequestDto dto, CancellationToken ct = default);
        Task<AddFeedCommentResponseDto> AddCommentAsync(AddFeedCommentRequestDto dto, CancellationToken ct = default);

        /// <summary>
        /// Fetches one comment as-is, with no translation attempt — meant only for showing a commenter
        /// their own just-posted comment instantly, which is always already in their language.
        /// </summary>
        Task<FeedCommentDto?> GetCommentAsync(int commentId, CancellationToken ct = default);

        Task<DeleteFeedCommentResponseDto> DeleteCommentAsync(int commentId, string requestingUserId, bool isModerator, CancellationToken ct = default);
        Task<ToggleFeedLikeResponseDto> ToggleLikeAsync(int postId, string userId, CancellationToken ct = default);

        /// <summary>Display names of who liked a post, newest first, for the like button's hover popup.</summary>
        Task<GetFeedLikersResponseDto> GetLikersAsync(int postId, CancellationToken ct = default);

        Task<FeedMediaFileDto?> GetMediaFileAsync(int mediaId, CancellationToken ct = default);
    }
}

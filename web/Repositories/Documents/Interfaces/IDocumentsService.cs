using web.Repositories.Documents.Dtos;

namespace web.Repositories.Documents.Interfaces
{
    public interface IDocumentsService
    {
        Task<List<DocumentGroupDto>> GetGroupsAsync(CancellationToken ct = default);

        Task<DocumentGroupDetailDto?> GetGroupDetailAsync(int groupId, string currentUserId, bool isModerator, CancellationToken ct = default);

        Task<CreateDocumentGroupResponseDto> CreateGroupAsync(CreateDocumentGroupRequestDto dto, CancellationToken ct = default);

        Task<UpdateDocumentGroupResponseDto> UpdateGroupAsync(UpdateDocumentGroupRequestDto dto, CancellationToken ct = default);

        Task<DeleteDocumentGroupResponseDto> DeleteGroupAsync(int groupId, string requestingUserId, bool isModerator, CancellationToken ct = default);

        Task<UploadDocumentsResponseDto> UploadDocumentsAsync(UploadDocumentsRequestDto dto, CancellationToken ct = default);

        Task<DeleteDocumentResponseDto> DeleteDocumentAsync(int documentId, string requestingUserId, bool isModerator, CancellationToken ct = default);

        Task<DocumentFileDto?> GetDocumentFileAsync(int documentId, CancellationToken ct = default);

        /// <param name="onProgress">
        /// Called once with the total chunk count as soon as the document's text has been extracted and
        /// split, then again after each chunk finishes translating with the number completed so far. Lets
        /// a caller running this in the background (see DocumentsController.TranslateStart) report "X af
        /// Y" progress without changing how chunks are translated (still strictly one at a time).
        /// </param>
        Task<TranslateDocumentResponseDto> TranslateDocumentAsync(
            int documentId,
            string preferredLanguageCode,
            bool force = false,
            Action<int>? onChunkCountKnown = null,
            Action<int>? onProgress = null,
            CancellationToken ct = default);
    }
}

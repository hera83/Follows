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
    }
}

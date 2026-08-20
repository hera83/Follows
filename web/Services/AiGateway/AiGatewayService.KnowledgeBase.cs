using web.Services.AiGateway.Dtos;
using web.Services.AiGateway.Dtos.KnowledgeBase;

namespace web.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<GroupResponseDto> CreateGroupAsync(CreateGroupRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<CreateGroupRequestDto, GroupResponseDto>(client, "KnowledgeBase/CreateGroup", request, cancellationToken);
    }

    public async Task<ListGroupsResponseDto> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<ListGroupsResponseDto>(client, "KnowledgeBase/ListGroups", cancellationToken);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        await DeleteAsync(client, $"KnowledgeBase/DeleteGroup/{id}", cancellationToken);
    }

    public async Task<DocumentResponseDto> UploadDocumentAsync(UploadDocumentRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        AddFormValue(content, "GroupId", request.GroupId.ToString());
        AddFilePart(content, "File", request.FileContent, request.FileName, request.ContentType);

        return await SendMultipartForJsonAsync<DocumentResponseDto>(client, "KnowledgeBase/UploadDocument", content, cancellationToken);
    }

    public async Task<ListDocumentsResponseDto> ListDocumentsAsync(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        var endpoint = "KnowledgeBase/ListDocuments";
        if (groupId.HasValue)
        {
            endpoint += $"?groupId={groupId.Value}";
        }

        return await GetAsync<ListDocumentsResponseDto>(client, endpoint, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        await DeleteAsync(client, $"KnowledgeBase/DeleteDocument/{id}", cancellationToken);
    }

    public async Task<AiGatewayFileResultDto> DownloadDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(cancellationToken);
        return await GetFileAsync(client, $"KnowledgeBase/DownloadDocument/{id}", cancellationToken);
    }

    public async Task<SearchResponseDto> SearchKnowledgeBaseAsync(SearchRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<SearchRequestDto, SearchResponseDto>(client, "KnowledgeBase/Search", request, cancellationToken);
    }

    public async Task<RagChatResponseDto> RagChatAsync(RagChatRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<RagChatRequestDto, RagChatResponseDto>(client, "KnowledgeBase/RagChat", request, cancellationToken);
    }
}

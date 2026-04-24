using ContosoDashboard.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ContosoDashboard.Services;

/// <summary>
/// Interface for document business logic operations
/// </summary>
public interface IDocumentService
{
    // Upload
    Task<Document> UploadAsync(DocumentUploadRequest request, int userId);

    // Retrieval
    Task<Document?> GetByIdAsync(int documentId, int userId);
    Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId, DocumentFilter? filter = null);
    Task<IEnumerable<Document>> GetProjectDocumentsAsync(int projectId, int userId);
    Task<IEnumerable<Document>> GetSharedWithMeAsync(int userId);

    // Search
    Task<IEnumerable<Document>> SearchAsync(string query, int userId);

    // Update
    Task<Document> UpdateAsync(int documentId, DocumentUpdateRequest request, int userId);
    Task<Document> ReplaceFileAsync(int documentId, IBrowserFile newFile, int userId);

    // Delete
    Task DeleteAsync(int documentId, int userId);

    // Sharing
    Task ShareAsync(int documentId, int recipientUserId, int userId);
    Task<IEnumerable<DocumentShare>> GetSharesAsync(int documentId);
}

/// <summary>
/// Request DTO for document upload
/// </summary>
public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public IBrowserFile File { get; set; } = null!;
    public int? ProjectId { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Request DTO for document metadata update
/// </summary>
public class DocumentUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
}

/// <summary>
/// Filter DTO for document queries
/// </summary>
public class DocumentFilter
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; }  // Title, CreatedAt, Category, FileSize
    public bool SortDescending { get; set; }
}
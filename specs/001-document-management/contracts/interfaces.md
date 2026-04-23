# Contracts: Document Upload and Management

**Feature**: Document Upload and Management  
**Date**: 2026-04-23

This document defines the public interfaces (contracts) for the document management feature. These contracts enable loose coupling, testability, and future cloud migration.

---

## Service Interfaces

### IDocumentService

Core document business logic interface.

```csharp
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
```

### IFileStorageService

Abstraction for file storage operations. Enables local/Azure storage swap.

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativePath);
    Task DeleteAsync(string relativePath);
    Task<Stream> DownloadAsync(string relativePath);
    Task<string> GetUrlAsync(string relativePath, TimeSpan expiration);
    Task<long> GetStorageUsedAsync(string basePath);
}
```

### IDocumentSearchService

Search functionality interface.

```csharp
public interface IDocumentSearchService
{
    Task<IEnumerable<Document>> SearchAsync(DocumentSearchRequest request, int userId);
    Task<IEnumerable<Document>> FilterByCategoryAsync(string category, int userId);
    Task<IEnumerable<Document>> FilterByProjectAsync(int projectId, int userId);
    Task<IEnumerable<Document>> FilterByDateRangeAsync(DateTime start, DateTime end, int userId);
}
```

### IQuotaService

Storage quota management interface.

```csharp
public interface IQuotaService
{
    Task<bool> CanUploadAsync(int userId, long fileSize);
    Task<bool> IsTotalQuotaExceededAsync(long fileSize);
    Task<long> GetUserStorageUsedAsync(int userId);
    Task<long> GetUserStorageQuotaAsync(int userId);
    Task<long> GetTotalStorageUsedAsync();
    Task<long> GetTotalStorageQuotaAsync();
    Task UpdateUserStorageAsync(int userId, long delta);
}
```

---

## Request/Response DTOs

### DocumentUploadRequest

```csharp
public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public IBrowserFile File { get; set; } = null!;
    public int? ProjectId { get; set; }
    public string? Tags { get; set; }
}
```

### DocumentUpdateRequest

```csharp
public class DocumentUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
}
```

### DocumentFilter

```csharp
public class DocumentFilter
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; }  // Title, CreatedAt, Category, FileSize
    public bool SortDescending { get; set; }
}
```

### DocumentSearchRequest

```csharp
public class DocumentSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? SearchIn { get; set; }  // Title, Description, Tags, All
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
}
```

---

## API Endpoints (Controllers)

### DocumentController

| Method | Path | Description | Authorization |
|--------|------|-------------|---------------|
| GET | /api/documents | Get user's documents | Authenticated |
| GET | /api/documents/{id} | Get document by ID | Document owner, project member, or shared |
| POST | /api/documents/upload | Upload new document | Authenticated |
| PUT | /api/documents/{id} | Update document metadata | Document owner |
| PUT | /api/documents/{id}/replace | Replace document file | Document owner |
| DELETE | /api/documents/{id} | Delete document | Document owner or Project Manager |
| POST | /api/documents/{id}/share | Share document | Document owner |
| GET | /api/documents/search?q={query} | Search documents | Authenticated |
| GET | /api/documents/project/{projectId} | Get project documents | Project member |
| GET | /api/documents/shared | Get shared with me | Authenticated |
| GET | /api/documents/download/{id} | Download document | Document access |

---

## Integration Points

### With Existing Services

- **IUserService**: Get user information, check permissions
- **IProjectService**: Validate project membership, get project details
- **INotificationService**: Create notifications for share events
- **ApplicationDbContext**: Persist Document and DocumentShare entities

### With Authentication

- Uses existing `CustomAuthenticationStateProvider`
- User ID extracted from `AuthenticationState`
- Role checks: Owner, Project Manager, Team Lead, Employee

---

## File Signature Validation Contract

```csharp
public interface IFileSignatureValidator
{
    Task<bool> ValidateAsync(Stream fileStream, string expectedExtension);
    IReadOnlyList<string> AllowedExtensions { get; }
}
```

**Expected Signatures**:
| Extension | Magic Bytes (hex) |
|-----------|-------------------|
| pdf | 25 50 44 46 |
| docx | 50 4B 03 04 |
| xlsx | 50 4B 03 04 |
| pptx | 50 4B 03 04 |
| jpg/jpeg | FF D8 FF |
| png | 89 50 4E 47 |
| txt | Any text |
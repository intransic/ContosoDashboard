# Data Model: Document Upload and Management

**Feature**: Document Upload and Management  
**Date**: 2026-04-23  
**Input**: Feature specification and research findings

## Entities

### Document

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | Primary key, auto-increment | Unique document identifier |
| Title | string | Required, max 200 chars | Document display title |
| Description | string | Optional, max 1000 chars | Document description |
| Category | string | Required, max 50 chars | Category from predefined list |
| FileName | string | Required, max 255 chars | Original file name |
| FilePath | string | Required, max 500 chars | Storage path (GUID-based) |
| FileSize | long | Required, > 0 | File size in bytes |
| FileType | string | Required, max 255 chars | MIME type (e.g., "application/pdf") |
| Tags | string | Optional, max 500 chars | Comma-separated tags |
| UploadedById | int | Required, foreign key | User who uploaded |
| ProjectId | int | Optional, foreign key | Associated project |
| CreatedAt | DateTime | Required | Upload date/time |
| UpdatedAt | DateTime | Optional | Last modification date |

**Relationships**:
- Many-to-one with User (UploadedBy)
- Many-to-one with Project (optional)
- One-to-many with DocumentShare

**State Transitions**:
- Created → Active (after upload)
- Active → Deleted (after user delete)

---

### DocumentShare

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | Primary key, auto-increment | Unique share identifier |
| DocumentId | int | Required, foreign key | Shared document |
| SharedWithUserId | int | Required, foreign key | Recipient user |
| SharedByUserId | int | Required, foreign key | User who shared |
| SharedAt | DateTime | Required | Share date/time |

**Relationships**:
- Many-to-one with Document
- Many-to-one with User (SharedWith)
- Many-to-one with User (SharedBy)

---

### User (Extension)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| StorageUsed | long | Default 0 | Total bytes used by user |
| StorageQuota | long | Default 524288000 (500 MB) | User storage limit |

---

### AppSettings Extension

| Key | Type | Description |
|-----|------|-------------|
| DocumentSettings:MaxFileSize | int | Maximum upload size in bytes (default 26214400 = 25 MB) |
| DocumentSettings:TotalStorageQuota | long | Total system storage limit |
| DocumentSettings:AllowedFileExtensions | string | Comma-separated allowed extensions |
| DocumentSettings:QuotaWarningThreshold | int | Percentage threshold for admin warnings |

---

## Validation Rules

### Document Upload

- Title: Required, 1-200 characters
- Category: Required, must be one of: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other
- File: Required, must be one of allowed types (pdf, docx, doc, xlsx, xls, pptx, ppt, txt, jpg, jpeg, png)
- File size: Must be <= 25 MB
- File signature: Must match expected magic numbers for the declared type

### Document Edit

- Title: Required, 1-200 characters
- Category: Required, same list as upload
- Only document owner can edit

### Document Delete

- Document owner can delete their own documents
- Project Manager can delete any document in their managed projects
- Requires confirmation before deletion

### Share

- Only document owner can share
- Recipient must be a valid user
- Recipient receives in-app notification

---

## Database Schema (EF Core)

```csharp
// Document entity
public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int UploadedById { get; set; }
    public int? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public User UploadedBy { get; set; } = null!;
    public Project? Project { get; set; }
    public ICollection<DocumentShare> Shares { get; set; }
}

// DocumentShare entity
public class DocumentShare
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int SharedWithUserId { get; set; }
    public int SharedByUserId { get; set; }
    public DateTime SharedAt { get; set; }
    
    public Document Document { get; set; } = null!;
    public User SharedWith { get; set; } = null!;
    public User SharedBy { get; set; } = null!;
}
```

---

## File Storage Pattern

**Directory Structure**:
```
AppData/
└── uploads/
    └── {userId}/
        └── {projectId or "personal"}/
            └── {guid}.{extension}
```

**Path Generation**:
1. Generate unique GUID for filename
2. Determine subdirectory: projectId if associated, else "personal"
3. Construct path: `{userId}/{subdirectory}/{guid}.{extension}`

**Security**:
- Files stored outside wwwroot (not web-accessible directly)
- GUID-based filenames prevent path traversal
- Authorization check required before serving files
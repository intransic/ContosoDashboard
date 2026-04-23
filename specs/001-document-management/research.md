# Research: Document Upload and Management

**Feature**: Document Upload and Management  
**Date**: 2026-04-23  
**Input**: Technical Context from plan.md

## Research Topics

### Topic 1: File Signature Validation in .NET

**Question**: How to implement basic file signature (magic number) validation to reject dangerous file types?

**Research**: 
- .NET provides `System.IO.File` and binary reader to read file headers
- Common magic numbers: MZ (exe), PE (dll), 4D 5A (exe), 5A 4D (zip)
- Can validate first 2-8 bytes against whitelist of safe file signatures

**Decision**: Implement `FileSignatureValidator` class that reads file header bytes and validates against known safe signatures for PDF, Office documents, text files, and images.

**Alternatives considered**:
- Use external library (rejected - adds dependency)
- Trust file extension only (rejected - insufficient security)

---

### Topic 2: Blazor Server File Upload Best Practices

**Question**: How to handle file uploads in Blazor Server without memory disposal issues?

**Research**:
- Blazor Server uses SignalR - file uploads use JS interop to stream to server
- Use `InputFile` component with `OnChange` handler
- Use `IBrowserFile.OpenReadStream()` with MemoryStream to avoid disposal issues
- Configure max file size in `Program.cs` or component

**Decision**: Use standard `InputFile` component with MemoryStream pattern. Set `maxAllowedSize` parameter to 25 MB per requirements.

**Alternatives considered**:
- JavaScript interop upload (rejected - adds complexity)
- Streaming with PipeReader (rejected - over-engineered for training)

---

### Topic 3: Document Search Implementation

**Question**: How to implement document search with filtering by multiple fields?

**Research**:
- Entity Framework Core supports LINQ-based filtering
- For simple search: `Where(d => d.Title.Contains(query) || d.Description.Contains(query))`
- For better performance: Full-text search or database-specific features
- For training: LINQ-based approach is sufficient and clear

**Decision**: Implement LINQ-based search with `IQueryable` to allow database-level filtering. Support search by title, description, tags, uploader name, and project name.

**Alternatives considered**:
- Full-text search (rejected - requires SQL Server configuration)
- ElasticSearch/Algolia (rejected - external service, not offline)

---

### Topic 4: Storage Quota Implementation

**Question**: How to implement per-user and total storage quotas?

**Research**:
- Track storage usage in database: User has `TotalStorageUsed` field
- Check quota before upload: `if (userStorageUsed + fileSize > userQuota) reject`
- Total quota: Sum all users' storage or track in app settings
- Store file sizes in Document entity

**Decision**: Add `StorageUsed` field to User entity. Check quota in DocumentService before allowing upload. Total quota stored in app settings.

**Alternatives considered**:
- Calculate storage on-the-fly by scanning files (rejected - slow)
- Use database triggers (rejected - adds complexity)

---

## Consolidated Findings

All research topics resolved. No remaining unknowns. Implementation approach confirmed:

1. **File validation**: Custom `FileSignatureValidator` using binary header inspection
2. **Upload**: Standard Blazor `InputFile` with MemoryStream pattern
3. **Search**: LINQ-based filtering with IQueryable for database efficiency
4. **Quotas**: Database-tracked storage with pre-upload validation

These approaches align with ContosoDashboard's existing patterns and the Constitution principles (Training Integrity, Security by Design, Simplicity and Clarity).
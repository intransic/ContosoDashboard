# Quickstart: Document Upload and Management

**Feature**: Document Upload and Management  
**Date**: 2026-04-23

This guide helps developers get started with implementing the document management feature.

---

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- ContosoDashboard project cloned and building

---

## Implementation Order

### Phase 1: Data Layer

1. **Add Document and DocumentShare entities** to `Models/`
2. **Update ApplicationDbContext** with new DbSets
3. **Add StorageUsed and StorageQuota fields** to User entity
4. **Run EF Core migration** to update database

### Phase 2: Storage Layer

5. **Create IFileStorageService interface** in `Services/`
6. **Implement LocalFileStorageService** for local filesystem
7. **Create AppData/uploads directory** structure

### Phase 3: Business Logic Layer

8. **Create IDocumentService interface** in `Services/`
9. **Implement DocumentService** with all CRUD operations
10. **Implement IQuotaService** for storage quotas
11. **Implement IFileSignatureValidator** for security

### Phase 4: API Layer

12. **Create DocumentController** for file download/preview
13. **Add authorization checks** to prevent IDOR

### Phase 5: UI Layer

14. **Create Documents.razor** page (My Documents)
15. **Create DocumentUpload.razor** page
16. **Create ProjectDocuments.razor** component
17. **Add Recent Documents widget** to dashboard

### Phase 6: Integration

18. **Add notification integration** for share events
19. **Add task document attachment** feature
20. **Test end-to-end flows**

---

## Key Files to Create

| File | Purpose |
|------|---------|
| `Models/Document.cs` | Document entity |
| `Models/DocumentShare.cs` | Share relationship entity |
| `Services/IFileStorageService.cs` | Storage abstraction |
| `Services/LocalFileStorageService.cs` | Local implementation |
| `Services/IDocumentService.cs` | Business logic interface |
| `Services/DocumentService.cs` | Business logic implementation |
| `Services/IQuotaService.cs` | Quota management |
| `Services/QuotaService.cs` | Quota implementation |
| `Services/FileSignatureValidator.cs` | Security validation |
| `Controllers/DocumentController.cs` | File endpoints |
| `Pages/Documents.razor` | Document list UI |
| `Pages/DocumentUpload.razor` | Upload UI |

---

## Configuration

Add to `appsettings.json`:

```json
{
  "DocumentSettings": {
    "MaxFileSize": 26214400,
    "TotalStorageQuota": 10737418240,
    "AllowedFileExtensions": "pdf,docx,doc,xlsx,xls,pptx,ppt,txt,jpg,jpeg,png",
    "QuotaWarningThreshold": 80
  }
}
```

---

## Testing Checklist

- [ ] Upload valid document succeeds
- [ ] Upload invalid file type is rejected
- [ ] Upload file > 25 MB is rejected
- [ ] Document appears in My Documents
- [ ] Document can be filtered by category
- [ ] Document can be filtered by project
- [ ] Document can be searched by title
- [ ] Document can be previewed (PDF, images)
- [ ] Document can be downloaded
- [ ] Document owner can edit metadata
- [ ] Document owner can delete document
- [ ] Document can be shared with another user
- [ ] Shared user receives notification
- [ ] Shared user can access document
- [ ] Per-user quota is enforced
- [ ] Unauthorized access is denied (IDOR)

---

## Common Issues

### File Not Found on Download

- Check that file exists in `AppData/uploads/` directory
- Verify `FilePath` in database matches actual path
- Ensure authorization check passes before serving

### Upload Fails with Memory Error

- Use `IBrowserFile.OpenReadStream()` with MemoryStream pattern
- Don't dispose the stream prematurely in Blazor

### Quota Not Enforced

- Verify `StorageUsed` is updated after upload
- Check `CanUploadAsync` is called before upload

---

## Next Steps

After implementation, run the feature spec acceptance scenarios to verify completeness. See `spec.md` for full acceptance criteria.
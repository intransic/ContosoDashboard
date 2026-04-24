using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContosoDashboard.Services;

/// <summary>
/// Document business logic implementation
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IQuotaService _quotaService;
    private readonly FileSignatureValidator _fileValidator;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<DocumentService> _logger;
    private readonly long _maxFileSize;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IQuotaService quotaService,
        FileSignatureValidator fileValidator,
        INotificationService? notificationService,
        IConfiguration configuration,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _quotaService = quotaService;
        _fileValidator = fileValidator;
        _notificationService = notificationService;
        _logger = logger;
        
        _maxFileSize = configuration.GetValue<long>("DocumentSettings:MaxFileSize", 26214400);
    }

    public async Task<Document> UploadAsync(DocumentUploadRequest request, int userId)
    {
        // Validate file size
        if (request.File.Size > _maxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB");
        }

        // Check user quota
        if (!await _quotaService.CanUploadAsync(userId, request.File.Size))
        {
            throw new InvalidOperationException("Storage quota exceeded. Please delete some files or contact your administrator.");
        }

        // Validate file signature
        var extension = Path.GetExtension(request.File.Name).TrimStart('.').ToLowerInvariant();
        using var stream = request.File.OpenReadStream(_maxFileSize);
        if (!await _fileValidator.ValidateAsync(stream, extension))
        {
            throw new InvalidOperationException("File type not allowed or file signature validation failed.");
        }

        // Generate unique file path
        var userIdStr = userId.ToString();
        var projectIdStr = request.ProjectId?.ToString() ?? "personal";
        var guid = Guid.NewGuid();
        var relativePath = $"{userIdStr}/{projectIdStr}/{guid}.{extension}";

        // Upload file to storage
        stream.Position = 0;
        await _fileStorageService.UploadAsync(stream, request.File.Name, request.File.ContentType, relativePath);

        // Create document record
        var document = new Document
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            FileName = request.File.Name,
            FilePath = relativePath,
            FileSize = request.File.Size,
            FileType = request.File.ContentType,
            Tags = request.Tags,
            UploadedById = userId,
            ProjectId = request.ProjectId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Update user storage quota
        await _quotaService.UpdateUserStorageAsync(userId, request.File.Size);

        _logger.LogInformation("Document {Title} uploaded by user {UserId}", document.Title, userId);
        
        return document;
    }

    public async Task<Document?> GetByIdAsync(int documentId, int userId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedBy)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return null;

        // Check authorization
        if (!await CanUserAccessDocumentAsync(document, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to access this document.");
        }

        return document;
    }

    public async Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId, DocumentFilter? filter = null)
    {
        var query = _context.Documents
            .Include(d => d.UploadedBy)
            .Where(d => d.UploadedById == userId);

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(d => d.Category == filter.Category);
            }

            if (filter.ProjectId.HasValue)
            {
                query = query.Where(d => d.ProjectId == filter.ProjectId);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= filter.StartDate);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= filter.EndDate);
            }

            query = filter.SortBy switch
            {
                "Title" => filter.SortDescending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
                "Category" => filter.SortDescending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
                "FileSize" => filter.SortDescending ? query.OrderByDescending(d => d.FileSize) : query.OrderBy(d => d.FileSize),
                _ => filter.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(d => d.CreatedAt);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetProjectDocumentsAsync(int projectId, int userId)
    {
        // Check if user is a member of the project
        var isMember = await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        var isProjectManager = await _context.Projects
            .AnyAsync(p => p.Id == projectId && p.ProjectManagerId == userId);

        if (!isMember && !isProjectManager)
        {
            throw new UnauthorizedAccessException("You do not have permission to view this project's documents.");
        }

        return await _context.Documents
            .Include(d => d.UploadedBy)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetSharedWithMeAsync(int userId)
    {
        return await _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Include(ds => ds.Document)
                .ThenInclude(d => d.UploadedBy)
            .Select(ds => ds.Document)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> SearchAsync(string query, int userId)
    {
        var lowerQuery = query.ToLowerInvariant();

        // Get user's own documents
        var userDocIds = await _context.Documents
            .Where(d => d.UploadedById == userId)
            .Select(d => d.Id)
            .ToListAsync();

        // Get shared document IDs
        var sharedDocIds = await _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Select(ds => ds.DocumentId)
            .ToListAsync();

        // Get project document IDs where user is a member
        var memberProjectIds = await _context.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        var projectDocIds = await _context.Documents
            .Where(d => d.ProjectId != null && memberProjectIds.Contains(d.ProjectId.Value))
            .Select(d => d.Id)
            .ToListAsync();

        // Combine all accessible document IDs
        var accessibleIds = userDocIds.Concat(sharedDocIds).Concat(projectDocIds).Distinct();

        // Search across title, description, tags
        return await _context.Documents
            .Include(d => d.UploadedBy)
            .Where(d => accessibleIds.Contains(d.Id))
            .Where(d => d.Title.ToLower().Contains(lowerQuery) ||
                       (d.Description != null && d.Description.ToLower().Contains(lowerQuery)) ||
                       (d.Tags != null && d.Tags.ToLower().Contains(lowerQuery)) ||
                       d.UploadedBy.DisplayName.ToLower().Contains(lowerQuery))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document> UpdateAsync(int documentId, DocumentUpdateRequest request, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            throw new InvalidOperationException("Document not found.");

        if (document.UploadedById != userId)
        {
            throw new UnauthorizedAccessException("You can only edit your own documents.");
        }

        document.Title = request.Title;
        document.Description = request.Description;
        document.Category = request.Category;
        document.Tags = request.Tags;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {Id} updated by user {UserId}", documentId, userId);
        
        return document;
    }

    public async Task<Document> ReplaceFileAsync(int documentId, IBrowserFile newFile, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            throw new InvalidOperationException("Document not found.");

        if (document.UploadedById != userId)
        {
            throw new UnauthorizedAccessException("You can only replace files in your own documents.");
        }

        // Validate new file
        if (newFile.Size > _maxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB");
        }

        // Check quota (accounting for old file size)
        var sizeDifference = newFile.Size - document.FileSize;
        if (sizeDifference > 0 && !await _quotaService.CanUploadAsync(userId, sizeDifference))
        {
            throw new InvalidOperationException("Storage quota exceeded.");
        }

        // Delete old file
        await _fileStorageService.DeleteAsync(document.FilePath);

        // Upload new file
        var extension = Path.GetExtension(newFile.Name).TrimStart('.').ToLowerInvariant();
        var userIdStr = userId.ToString();
        var projectIdStr = document.ProjectId?.ToString() ?? "personal";
        var guid = Guid.NewGuid();
        var newRelativePath = $"{userIdStr}/{projectIdStr}/{guid}.{extension}";

        using var stream = newFile.OpenReadStream(_maxFileSize);
        await _fileStorageService.UploadAsync(stream, newFile.Name, newFile.ContentType, newRelativePath);

        // Update document record
        document.FileName = newFile.Name;
        document.FilePath = newRelativePath;
        document.FileSize = newFile.Size;
        document.FileType = newFile.ContentType;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update quota
        await _quotaService.UpdateUserStorageAsync(userId, sizeDifference);

        _logger.LogInformation("Document {Id} file replaced by user {UserId}", documentId, userId);
        
        return document;
    }

    public async Task DeleteAsync(int documentId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            throw new InvalidOperationException("Document not found.");

        // Check authorization: owner or Project Manager
        var isOwner = document.UploadedById == userId;
        var isProjectManager = false;
        
        if (document.ProjectId.HasValue)
        {
            isProjectManager = await _context.Projects
                .AnyAsync(p => p.Id == document.ProjectId && p.ProjectManagerId == userId);
        }

        if (!isOwner && !isProjectManager)
        {
            throw new UnauthorizedAccessException("You can only delete your own documents or documents in your projects.");
        }

        // Delete file
        await _fileStorageService.DeleteAsync(document.FilePath);

        // Update quota
        await _quotaService.UpdateUserStorageAsync(userId, -document.FileSize);

        // Delete document record (cascade will handle shares)
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {Id} deleted by user {UserId}", documentId, userId);
    }

    public async Task ShareAsync(int documentId, int recipientUserId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        if (document == null)
            throw new InvalidOperationException("Document not found.");

        if (document.UploadedById != userId)
        {
            throw new UnauthorizedAccessException("You can only share your own documents.");
        }

        // Check if already shared
        var existingShare = await _context.DocumentShares
            .AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == recipientUserId);

        if (existingShare)
        {
            throw new InvalidOperationException("Document already shared with this user.");
        }

        var share = new DocumentShare
        {
            DocumentId = documentId,
            SharedWithUserId = recipientUserId,
            SharedByUserId = userId,
            SharedAt = DateTime.UtcNow
        };

        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync();

        // Create notification
        if (_notificationService != null)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = recipientUserId,
                Title = "Document Shared",
                Message = $"A document '{document.Title}' has been shared with you.",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Informational,
                Info = $"DocumentId: {documentId}"
            });
        }

        _logger.LogInformation("Document {Id} shared with user {RecipientId} by user {UserId}", 
            documentId, recipientUserId, userId);
    }

    public async Task<IEnumerable<DocumentShare>> GetSharesAsync(int documentId)
    {
        return await _context.DocumentShares
            .Include(ds => ds.SharedWith)
            .Where(ds => ds.DocumentId == documentId)
            .ToListAsync();
    }

    private async Task<bool> CanUserAccessDocumentAsync(Document document, int userId)
    {
        // Owner can always access
        if (document.UploadedById == userId)
            return true;

        // Check if shared with user
        var isShared = await _context.DocumentShares
            .AnyAsync(ds => ds.DocumentId == document.Id && ds.SharedWithUserId == userId);
        if (isShared)
            return true;

        // Check if user is a project member
        if (document.ProjectId.HasValue)
        {
            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == document.ProjectId && pm.UserId == userId);
            if (isMember)
                return true;
        }

        return false;
    }
}
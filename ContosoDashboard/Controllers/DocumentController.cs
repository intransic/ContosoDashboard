using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

/// <summary>
/// API controller for document management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    private int GetUserId()
    {
        // Extract user ID from claims (mock authentication)
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        
        // Fallback for mock auth - try to get from Name identifier
        if (int.TryParse(User?.Identity?.Name, out var fallbackUserId))
        {
            return fallbackUserId;
        }
        
        // Default for testing
        return 1;
    }

    /// <summary>
    /// Get user's documents
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? category, [FromQuery] int? projectId)
    {
        try
        {
            var userId = GetUserId();
            var filter = new DocumentFilter
            {
                Category = category,
                ProjectId = projectId
            };
            
            var documents = await _documentService.GetUserDocumentsAsync(userId, filter);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.GetByIdAsync(id, userId);
            
            if (document == null)
                return NotFound();
            
            return Ok(document);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadRequest request)
    {
        try
        {
            var userId = GetUserId();
            
            // Map IFormFile to IBrowserFile
            var browserFile = new FormFileAdapter((IFormFile)request.File);
            request.File = browserFile;
            
            var document = await _documentService.UploadAsync(request, userId);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update document metadata
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(int id, [FromBody] DocumentUpdateRequest request)
    {
        try
        {
            var userId = GetUserId();
            var document = await _documentService.UpdateAsync(id, request, userId);
            return Ok(document);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Replace document file
    /// </summary>
    [HttpPut("{id}/replace")]
    public async Task<IActionResult> ReplaceFile(int id, IFormFile file)
    {
        try
        {
            var userId = GetUserId();
            var browserFile = new FormFileAdapter(file);
            var document = await _documentService.ReplaceFileAsync(id, browserFile, userId);
            return Ok(document);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing document {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete document
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search documents
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            var userId = GetUserId();
            var documents = await _documentService.SearchAsync(q, userId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get project documents
    /// </summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectDocuments(int projectId)
    {
        try
        {
            var userId = GetUserId();
            var documents = await _documentService.GetProjectDocumentsAsync(projectId, userId);
            return Ok(documents);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get documents shared with current user
    /// </summary>
    [HttpGet("shared")]
    public async Task<IActionResult> GetSharedDocuments()
    {
        try
        {
            var userId = GetUserId();
            var documents = await _documentService.GetSharedWithMeAsync(userId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Share document with a user
    /// </summary>
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareDocument(int id, [FromBody] ShareRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _documentService.ShareAsync(id, request.RecipientUserId, userId);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing document {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download document file
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string? path, [FromQuery] int? id)
    {
        try
        {
            var userId = GetUserId();
            
            string? filePath = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                filePath = path;
            }
            else if (id.HasValue)
            {
                var document = await _documentService.GetByIdAsync(id.Value, userId);
                if (document == null)
                    return NotFound();
                filePath = document.FilePath;
            }
            
            if (string.IsNullOrEmpty(filePath))
                return BadRequest("Either path or id must be provided");
            
            var stream = await _fileStorageService.DownloadAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return File(stream, "application/octet-stream", fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO for sharing documents
/// </summary>
public class ShareRequest
{
    public int RecipientUserId { get; set; }
}

/// <summary>
/// Adapter to convert IFormFile to IBrowserFile interface
/// </summary>
public class FormFileAdapter : IBrowserFile
{
    private readonly IFormFile _formFile;

    public FormFileAdapter(IFormFile formFile)
    {
        _formFile = formFile;
    }

    public string Name => _formFile.FileName;
    public long Size => _formFile.Length;
    public string ContentType => _formFile.ContentType;
    public DateTimeOffset LastModified => DateTimeOffset.Now;
    public DateTimeOffset? ServerTime => null;

    public Stream OpenReadStream(long maxAllowedSize = 512000)
    {
        return _formFile.OpenReadStream();
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
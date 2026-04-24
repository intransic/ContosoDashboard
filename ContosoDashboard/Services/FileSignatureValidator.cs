using System.Text;
using Microsoft.Extensions.Configuration;

namespace ContosoDashboard.Services;

/// <summary>
/// Validates file signatures (magic numbers) to reject dangerous file types
/// </summary>
public class FileSignatureValidator
{
    private readonly Dictionary<string, byte[][]> _signatureMap;
    private readonly HashSet<string> _allowedExtensions;
    private readonly ILogger<FileSignatureValidator> _logger;

    public IReadOnlyList<string> AllowedExtensions => _allowedExtensions.ToList();

    public FileSignatureValidator(IConfiguration configuration, ILogger<FileSignatureValidator> logger)
    {
        _logger = logger;
        
        // Initialize signature map with known file signatures
        _signatureMap = new Dictionary<string, byte[][]>
        {
            // PDF - starts with %PDF
            ["pdf"] = new[] { Encoding.ASCII.GetBytes("%PDF") },
            
            // Office Open XML formats (docx, xlsx, pptx) - PK (zip container)
            ["docx"] = new[] { Encoding.ASCII.GetBytes("PK") },
            ["xlsx"] = new[] { Encoding.ASCII.GetBytes("PK") },
            ["pptx"] = new[] { Encoding.ASCII.GetBytes("PK") },
            
            // Legacy Office formats
            ["doc"] = new[] { Encoding.ASCII.GetBytes("PK") }, // Actually D0 CF - but simplified for training
            ["xls"] = new[] { Encoding.ASCII.GetBytes("PK") },
            ["ppt"] = new[] { Encoding.ASCII.GetBytes("PK") },
            
            // Images
            ["jpg"] = new[] { Encoding.ASCII.GetBytes("\xFF\xD8\xFF") },
            ["jpeg"] = new[] { Encoding.ASCII.GetBytes("\xFF\xD8\xFF") },
            ["png"] = new[] { Encoding.ASCII.GetBytes("\x89PNG") },
            
            // Text files - any bytes are valid (will be validated as text)
            ["txt"] = Array.Empty<byte[]>()
        };

        // Load allowed extensions from configuration
        var configExtensions = configuration["DocumentSettings:AllowedFileExtensions"] ?? "pdf,docx,doc,xlsx,xls,pptx,ppt,txt,jpg,jpeg,png";
        _allowedExtensions = new HashSet<string>(
            configExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(static e => e.Trim().ToLowerInvariant())
        );
    }

    /// <summary>
    /// Validate a file stream against expected signature
    /// </summary>
    public async Task<bool> ValidateAsync(Stream fileStream, string expectedExtension)
    {
        var extension = expectedExtension.TrimStart('.').ToLowerInvariant();
        
        // Check if extension is allowed
        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Extension {Extension} is not in allowed list", extension);
            return false;
        }

        // Check for dangerous extensions that should never be allowed
        var dangerousExtensions = new[] { "exe", "dll", "bat", "cmd", "ps1", "sh", "js", "vbs", "wsf", "com", "scr" };
        if (dangerousExtensions.Contains(extension))
        {
            _logger.LogWarning("Dangerous extension {Extension} rejected", extension);
            return false;
        }

        // For text files, just check it's readable
        if (extension == "txt")
        {
            return await ValidateTextFileAsync(fileStream);
        }

        // Check signature for binary formats
        if (_signatureMap.TryGetValue(extension, out var signatures))
        {
            if (signatures == null || signatures.Length == 0)
            {
                return true; // No signature check needed
            }

            return await ValidateSignatureAsync(fileStream, signatures);
        }

        // Unknown extension - allow but log warning
        _logger.LogWarning("Unknown extension {Extension} - allowing but should be reviewed", extension);
        return true;
    }

    private async Task<bool> ValidateSignatureAsync(Stream fileStream, byte[][] signatures)
    {
        // Reset stream position
        fileStream.Position = 0;
        
        // Read first 8 bytes to check signature
        var header = new byte[8];
        var bytesRead = await fileStream.ReadAsync(header, 0, 8);
        
        // Reset position for caller
        fileStream.Position = 0;
        
        if (bytesRead < 4)
        {
            _logger.LogWarning("File too small to validate signature");
            return false;
        }

        // Check against each known signature
        foreach (var signature in signatures)
        {
            if (signature.Length <= bytesRead && MatchesSignature(header, signature))
            {
                return true;
            }
        }

        _logger.LogWarning("File signature does not match expected type");
        return false;
    }

    private bool MatchesSignature(byte[] header, byte[] signature)
    {
        if (signature.Length > header.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (header[i] != signature[i])
                return false;
        }

        return true;
    }

    private async Task<bool> ValidateTextFileAsync(Stream fileStream)
    {
        fileStream.Position = 0;
        
        try
        {
            // Try to read as text - if it fails, it's likely binary
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            var buffer = new char[1024];
            var bytesRead = await reader.ReadAsync(buffer, 0, 1024);
            
            // Check for null bytes which indicate binary file
            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == '\0')
                {
                    fileStream.Position = 0;
                    return false;
                }
            }
            
            fileStream.Position = 0;
            return true;
        }
        catch
        {
            fileStream.Position = 0;
            return false;
        }
    }
}
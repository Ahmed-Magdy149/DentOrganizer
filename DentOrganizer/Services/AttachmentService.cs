using Microsoft.AspNetCore.Components.Forms;

namespace DentOrganizer.Services;

/// <summary>Stores session files (X-rays, photos, PDFs) under the app data folder: attachments/{sessionId}/.</summary>
public class AttachmentService(IDbContextFactory<AppDbContext> dbFactory)
{
    public const long MaxFileSize = 100L * 1024 * 1024; // 100 MB

    public static string RootFolder => Path.Combine(FileSystem.AppDataDirectory, "attachments");

    private static string SessionFolder(int sessionId) => Path.Combine(RootFolder, sessionId.ToString());

    public static string GetPath(SessionAttachment a) => Path.Combine(SessionFolder(a.SessionId), a.StoredFileName);

    public static AttachmentKind GuessKind(string? contentType, string fileName)
    {
        if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) return AttachmentKind.Photo;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".dcm" => AttachmentKind.XRay,
            ".pdf" or ".doc" or ".docx" => AttachmentKind.Document,
            _ => AttachmentKind.Other,
        };
    }

    public async Task<SessionAttachment> SaveAsync(int sessionId, IBrowserFile file, AttachmentKind? kind, string? description)
    {
        var folder = SessionFolder(sessionId);
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.Name);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(folder, stored);

        await using (var src = file.OpenReadStream(MaxFileSize))
        await using (var dst = File.Create(path))
        {
            await src.CopyToAsync(dst);
        }

        var attachment = new SessionAttachment
        {
            SessionId = sessionId,
            Kind = kind ?? GuessKind(file.ContentType, file.Name),
            OriginalFileName = file.Name,
            StoredFileName = stored,
            ContentType = string.IsNullOrEmpty(file.ContentType) ? null : file.ContentType,
            SizeBytes = file.Size,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };

        // Small JPEG thumbnail, resized by the browser engine, so lists stay fast.
        if (attachment.IsImage)
        {
            try
            {
                var thumb = await file.RequestImageFileAsync("image/jpeg", 360, 360);
                var thumbName = $"thumb_{Path.GetFileNameWithoutExtension(stored)}.jpg";
                await using var src = thumb.OpenReadStream(MaxFileSize);
                await using var dst = File.Create(Path.Combine(folder, thumbName));
                await src.CopyToAsync(dst);
                attachment.ThumbnailFileName = thumbName;
            }
            catch
            {
                // Unsupported image format for the browser engine: no thumbnail, file is still saved.
            }
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        db.SessionAttachments.Add(attachment);
        await db.SaveChangesAsync();
        return attachment;
    }

    public async Task<string?> GetThumbnailDataUrlAsync(SessionAttachment a)
    {
        if (a.ThumbnailFileName is null) return null;
        var path = Path.Combine(SessionFolder(a.SessionId), a.ThumbnailFileName);
        if (!File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path);
        return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
    }

    /// <summary>Opens the file with the default app of the OS (image viewer, PDF reader, ...).</summary>
    public async Task OpenAsync(SessionAttachment a)
    {
        var path = GetPath(a);
        if (!File.Exists(path)) throw new FileNotFoundException("The file was not found on disk.", path);
        await Launcher.Default.OpenAsync(new OpenFileRequest(a.OriginalFileName, a.ContentType is null ? new ReadOnlyFile(path) : new ReadOnlyFile(path, a.ContentType)));
    }

    public async Task DeleteAsync(int attachmentId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var a = await db.SessionAttachments.FindAsync(attachmentId);
        if (a is null) return;
        db.SessionAttachments.Remove(a);
        await db.SaveChangesAsync();

        TryDelete(GetPath(a));
        if (a.ThumbnailFileName is not null)
            TryDelete(Path.Combine(SessionFolder(a.SessionId), a.ThumbnailFileName));
    }

    public void DeleteSessionFolder(int sessionId)
    {
        try
        {
            var folder = SessionFolder(sessionId);
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

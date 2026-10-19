using Microsoft.AspNetCore.Components.Forms;

namespace FamilyTree.Services;

// Stores uploaded photos in the "photos" folder (outside git, like the database) served at /photos
public class PhotoStorage(IWebHostEnvironment env)
{
    public const string RequestPath = "/photos";
    const long MaxBytes = 15 * 1024 * 1024;
    static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public string Folder { get; } = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "photos")).FullName;

    public async Task<string> SaveAsync(IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Extensions.Contains(ext) || !file.ContentType.StartsWith("image/"))
            throw new InvalidOperationException("Envie uma imagem (jpg, png, webp ou gif).");
        if (file.Size > MaxBytes)
            throw new InvalidOperationException("A foto deve ter no máximo 15 MB.");

        var name = $"{Guid.NewGuid():N}{ext}";
        await using var target = File.Create(Path.Combine(Folder, name));
        await file.OpenReadStream(MaxBytes).CopyToAsync(target);
        return $"{RequestPath}/{name}";
    }
}

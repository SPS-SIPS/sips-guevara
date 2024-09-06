namespace SPSS.Infrastructure.Services;

public sealed class FileService : IFileStorage
{
    public Task DeleteFile(string fileRoute, string containerName)
    {
        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), containerName, fileRoute));
        return Task.FromResult(0);
    }

    public Task<byte[]> GetFile(string fileRoute, string containerName)
    {
        return File.ReadAllBytesAsync(Path.Combine(Directory.GetCurrentDirectory(), containerName, fileRoute));
    }

    public Task<string> SaveFile(byte[] content, string extension, string containerName)
    {
        var fileName = $"{Guid.NewGuid()}{extension}";
        var folder = Path.Combine(Directory.GetCurrentDirectory(), containerName);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var route = Path.Combine(folder, fileName);
        File.WriteAllBytes(route, content);

        return Task.FromResult(route);
    }
}
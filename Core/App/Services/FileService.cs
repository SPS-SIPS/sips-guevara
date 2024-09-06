using System.Text;

namespace SPSS.App.Services;

public static class FileService
{
    public static MemoryStream? GetFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        return new MemoryStream(File.ReadAllBytes(path));
    }

    public static string? GetBase64File(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        var file = File.ReadAllBytes(path);

        return Convert.ToBase64String(file);
    }
    public static async Task SaveFile(string path, MemoryStream content)
    {
        using var stream = new FileStream(path, FileMode.Create);
        await content.CopyToAsync(stream);
    }

    public static async Task SaveFile(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.Create);

        var bytes = Convert.FromBase64String(content);
        var ms = new MemoryStream(bytes);

        await ms.CopyToAsync(stream);
    }

    public static void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public static void CreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            return;
        }
        Directory.CreateDirectory(path);
    }

    public static void DeleteDirectory(string path)
    {
        Directory.Delete(path, true);
    }

    public static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            return;
        }
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
    public static void IsValidPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception("Please add block storage path at: FileConfig.Path");
        }
    }

    public static string ConvertMemoryStreamToString(MemoryStream memoryStream)
    {
        memoryStream.Position = 0;

        using StreamReader reader = new(memoryStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

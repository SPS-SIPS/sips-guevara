namespace SPSS.Domain.Common;
public static class FS
{
    public static bool IsValidImageExtension(string extension)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".gif", ".svg"];
        return validExtensions.Contains(extension.ToLower());
    }
}

namespace SPSS.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> SaveFile(byte[] content, string extension, string containerName);
    Task DeleteFile(string fileRoute, string containerName);
    Task<byte[]> GetFile(string fileRoute, string containerName);
}
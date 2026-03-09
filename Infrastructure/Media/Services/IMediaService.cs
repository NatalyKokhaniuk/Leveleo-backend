namespace LeveLEO.Infrastructure.Media.Services;

public interface IMediaService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

    Task DeleteFileAsync(string fileKey);

    Task<string> GetFileUrlAsync(string key, TimeSpan? expiresIn = null);

    string GetPermanentUrl(string key);

    Task ClearBucketAsync();
}
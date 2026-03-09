namespace LeveLEO.Infrastructure.Media.Services;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MediaService : IMediaService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _baseUrl;

    public MediaService(IConfiguration config)
    {
        var accessKey = config["Media:AccessKey"];
        var secretKey = config["Media:SecretKey"];
        _bucket = config["Media:BucketName"] ?? throw new ArgumentNullException(nameof(config));
        _baseUrl = config["Media:BaseUrl"] ?? throw new ArgumentNullException(nameof(config));

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        var s3Config = new AmazonS3Config
        {
            ServiceURL = config["Media:ServiceUrl"],
            ForcePathStyle = true
        };

        _s3 = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string> UploadFileAsync(Stream file, string key, string contentType)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = file,
            ContentType = contentType
        });

        return key;
    }

    public async Task DeleteFileAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucket, key);
    }

    // === Pre-signed URL метод ===
    public Task<string> GetFileUrlAsync(string key, TimeSpan? expiresIn = null)
    {
        var expiry = expiresIn ?? TimeSpan.FromDays(365); // дефолт 365 дней

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        string url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);

        //return Task.FromResult(GetPermanentUrl(key));
    }

    public string GetPermanentUrl(string key) => $"{_baseUrl}/{key}";

    public async Task ClearBucketAsync()
    {
        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucket
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _s3.ListObjectsV2Async(listRequest);

                // Защита от возможного null у S3Objects
                if (listResponse.S3Objects != null && listResponse.S3Objects.Count != 0)
                {
                    var deleteObjects = listResponse.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList();

                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucket,
                        Objects = deleteObjects
                    };

                    await _s3.DeleteObjectsAsync(deleteRequest);
                    Console.WriteLine($"Видалено {deleteObjects.Count} об'єктів.");
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
                // Безопасная проверка nullable булевого значения
            } while (listResponse.IsTruncated == true);

            Console.WriteLine("Бакет очищено повністю.");
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"S3 помилка при очищенні: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Загальна помилка при очищенні бакету: {ex.Message}");
            throw;
        }
    }
}
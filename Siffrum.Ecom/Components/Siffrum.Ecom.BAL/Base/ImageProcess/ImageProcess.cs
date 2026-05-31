using Amazon.S3;
using Amazon.S3.Model;
using AutoMapper;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.Config.Configuration;
using Siffrum.Ecom.DAL.Context;

namespace Siffrum.Ecom.BAL.Base.ImageProcess
{
    public class ImageProcess : SiffrumBalBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly S3Settings _s3;
        private readonly IAmazonS3 _s3Client;
        private readonly bool _useS3;

        public ImageProcess(IMapper mapper, ApiDbContext context, IWebHostEnvironment env, APIConfiguration config)
            : base(mapper, context)
        {
            _env = env;
            _s3 = config.S3Settings ?? new S3Settings();
            _useS3 = !string.IsNullOrEmpty(_s3.AccessKey) && !string.IsNullOrEmpty(_s3.SecretKey);

            if (_useS3)
            {
                _s3Client = new AmazonS3Client(
                    _s3.AccessKey,
                    _s3.SecretKey,
                    Amazon.RegionEndpoint.GetBySystemName(_s3.Region));
                Console.WriteLine($"[ImageProcess] ✅ S3 ENABLED — Bucket: {_s3.BucketName}, Region: {_s3.Region}");
            }
            else
            {
                Console.WriteLine($"[ImageProcess] ⚠️ S3 DISABLED — AccessKey empty: {string.IsNullOrEmpty(_s3.AccessKey)}, SecretKey empty: {string.IsNullOrEmpty(_s3.SecretKey)}. Images will save to LOCAL DISK!");
            }
        }

        public async Task<string?> SaveFromBase64(
            string base64String,
            string imageExtension = "jpg",
            string imagePath = @"content/loginusers/profile")
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return null;

            if (imagePath.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                imagePath = imagePath.Substring("wwwroot".Length).TrimStart('/', '\\');
            }

            // Strip data URI prefix if present (e.g. "data:image/png;base64,...")
            var commaIdx = base64String.IndexOf(',');
            if (commaIdx >= 0 && commaIdx < 100)
                base64String = base64String.Substring(commaIdx + 1);

            imageExtension = imageExtension?.Trim().Replace(".", "").ToLower();
            byte[] fileBytes = Convert.FromBase64String(base64String);

            int maxSize = imageExtension switch
            {
                "mp4" => 3 * 1024 * 1024,
                "svg" or "mp3" or "m4a" => 10 * 1024 * 1024,
                _ => 1 * 1024 * 1024
            };

            if (fileBytes.Length > maxSize)
            {
                throw new Exception($"File size exceeds {maxSize / (1024 * 1024)} MB limit.");
            }

            string fileName = $"{Guid.NewGuid()}.{imageExtension}";

            if (!_useS3)
            {
                throw new Exception("S3 is not configured. Image upload is not allowed without S3. Check S3Settings in environment variables.");
            }

            // ── S3 upload ──
            var s3Prefix = ResolveS3Prefix(imagePath, imageExtension);
            var s3Key = $"{s3Prefix}{fileName}";

            var contentType = ResolveContentType(imageExtension);

            using var stream = new MemoryStream(fileBytes);
            var putReq = new PutObjectRequest
            {
                BucketName = _s3.BucketName,
                Key = s3Key,
                InputStream = stream,
                ContentType = contentType,
            };

            await _s3Client.PutObjectAsync(putReq);

            return $"https://{_s3.BucketName}.s3.{_s3.Region}.amazonaws.com/{s3Key}";
        }

        public async Task<string?> ConvertToBase64(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            // S3/HTTP URL — return directly (frontend loads from URL)
            if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return filePath;
            }

            // Old local path — no longer supported, return null
            return null;
        }

        public record ImageResult(string? NetworkUrl, string? Base64);

        public async Task<ImageResult> ResolveImage(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return new ImageResult(null, null);

            var result = await ConvertToBase64(filePath);
            if (result == null)
                return new ImageResult(null, null);

            if (result.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || result.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new ImageResult(result, null);
            }

            return new ImageResult(null, result);
        }

        private static string ResolveS3Prefix(string imagePath, string ext)
        {
            // Audio files → instruction-audio/
            if (imagePath.Contains("audio", StringComparison.OrdinalIgnoreCase)
                || ext == "mp3" || ext == "m4a" || ext == "wav" || ext == "aac")
            {
                return "instruction-audio/";
            }

            // Everything else → uploads/<subfolder>/
            // Strip "content/" prefix to get clean subfolder: "content/products" → "products"
            var sub = imagePath
                .Replace("content/", "", StringComparison.OrdinalIgnoreCase)
                .Replace("content\\", "", StringComparison.OrdinalIgnoreCase)
                .Trim('/', '\\');

            if (string.IsNullOrEmpty(sub))
                return "uploads/";

            return $"uploads/{sub}/";
        }

        private static string ResolveContentType(string ext) => ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "svg" => "image/svg+xml",
            "webp" => "image/webp",
            "mp4" => "video/mp4",
            "mp3" => "audio/mpeg",
            "m4a" => "audio/mp4",
            "wav" => "audio/wav",
            "aac" => "audio/aac",
            _ => "application/octet-stream"
        };
    }
}

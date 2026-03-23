using CineBook.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;

namespace CineBook.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly ILogger<ImageService> _logger;
        private readonly Cloudinary _cloudinary;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedContentTypes =
            { "image/jpeg", "image/png", "image/webp" };

        public ImageService(ILogger<ImageService> logger, Cloudinary cloudinary)
        {
            _logger = logger;
            _cloudinary = cloudinary;
        }

        // ── Upload Image (movie posters) ──────────────────────
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            ValidateFile(file);

            _logger.LogInformation("📤 Uploading image: {FileName}", file.FileName);

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "cinebook-posters",
                PublicId = Guid.NewGuid().ToString()
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError("❌ Image upload error: {Error}", result.Error.Message);
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
            }

            _logger.LogInformation("✅ Image uploaded: {Url}", result.SecureUrl);
            return result.SecureUrl.ToString();
        }

        // ── Upload PDF as raw (kept for compatibility) ────────
        public async Task<string> UploadPdfBytesAsync(byte[] pdfBytes, string fileName)
        {
            _logger.LogInformation("📤 Uploading PDF: {FileName}", fileName);

            using var stream = new MemoryStream(pdfBytes);
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "cinebook-tickets",
                PublicId = Path.GetFileNameWithoutExtension(fileName),
                UseFilename = true,
                UniqueFilename = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"PDF upload failed: {result.Error.Message}");

            _logger.LogInformation("✅ PDF uploaded: {Url}", result.SecureUrl);
            return result.SecureUrl.ToString();
        }

        // ── Convert PDF to PNG image + upload to Cloudinary ───
        // ✅ Images work on Cloudinary free plan — raw PDFs don't!
        public async Task<string> UploadPdfAsImageAsync(byte[] pdfBytes, string fileNameWithoutExt)
        {
            _logger.LogInformation("🖼 Converting PDF to image: {FileName}", fileNameWithoutExt);

            try
            {
                // Convert first page of PDF to SKBitmap using PDFtoImage
                using var pdfStream = new MemoryStream(pdfBytes);

                // Render first page at high DPI for quality
                var bitmap = Conversion.ToImage(pdfStream, page: 0, options: new RenderOptions { Dpi = 150 });

                // Encode as PNG bytes
                using var imageStream = new MemoryStream();
                bitmap.Encode(imageStream, SKEncodedImageFormat.Png, 100);
                var imageBytes = imageStream.ToArray();

                _logger.LogInformation("✅ PDF converted to PNG ({Size} bytes)", imageBytes.Length);

                // Upload PNG to Cloudinary — images work fine on free plan!
                var imageName = fileNameWithoutExt + ".png";
                using var uploadStream = new MemoryStream(imageBytes);

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(imageName, uploadStream),
                    Folder = "cinebook-ticket-previews",
                    PublicId = fileNameWithoutExt,
                    UseFilename = true,
                    UniqueFilename = false,
                    // Optimize for WhatsApp sharing
                    Transformation = new Transformation()
                        .Width(1200).Crop("limit").Quality("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("❌ Ticket image upload failed: {Error}", result.Error.Message);
                    throw new Exception($"Ticket image upload failed: {result.Error.Message}");
                }

                _logger.LogInformation("✅ Ticket image uploaded: {Url}", result.SecureUrl);
                return result.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ PDF to image conversion failed for {FileName}", fileNameWithoutExt);
                throw;
            }
        }

        // ── Delete ────────────────────────────────────────────
        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
            _logger.LogInformation("🗑 Image deleted: {PublicId}", publicId);
        }

        // ── Validation ────────────────────────────────────────
        private static void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided");
            if (file.Length > MAX_FILE_SIZE)
                throw new ArgumentException("File too large. Maximum size is 5MB.");
            if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Only JPG, PNG, and WEBP formats are allowed.");
        }
    }
}
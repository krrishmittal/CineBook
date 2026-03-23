using Microsoft.AspNetCore.Http;

public interface IImageService
{
    Task<string> UploadImageAsync(IFormFile file);
    Task<string> UploadPdfAsImageAsync(byte[] pdfBytes, string fileName);
    Task DeleteImageAsync(string publicId);
}
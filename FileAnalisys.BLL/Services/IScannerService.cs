using FileAnalysis.BLL.Models;

namespace FileAnalysis.BLL.Services
{
    public interface IScannerService
    {
        Task<ScanModel> ScanFile(string url);
        Task<byte[]> GetContent(string url);
        string Hash(byte[] input);
        Task<string> SendRequest(byte[] content);
        Task<string> CheckCacheBeforeSend(string hashKey, byte[] file);
    }
}
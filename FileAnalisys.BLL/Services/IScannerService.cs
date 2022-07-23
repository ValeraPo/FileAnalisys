using FileAnalysis.BLL.Models;

namespace FileAnalysis.BLL.Services
{
    public interface IScannerService
    {
        Task<ScanModel> ScanFile(string url);
    }
}
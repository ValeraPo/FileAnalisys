using FileAnalysis.BLL.Models;

namespace FileAnalysis.BLL.Services
{
    public interface IScannerService
    {
        ScanModel ScanFile(string url);
    }
}
using FileAnalysis.BLL.Models;

namespace FileAnalysis.BLL.Services
{
    public interface IProcessService
    {
        ScanModel ScanFile(string url);
    }
}
using AutoMapper;
using FileAnalysis.API.Models;
using FileAnalysis.BLL.Models;

namespace FileAnalysis.API.Configuration
{
    // Mapping Request Models and Model from BLL
    public class AutoMapperApiBll : Profile
    {
        public AutoMapperApiBll()
        {
            CreateMap<ScanModel, ScanResponse>();
        }
    }
}

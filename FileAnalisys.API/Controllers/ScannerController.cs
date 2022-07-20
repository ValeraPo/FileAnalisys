using AutoMapper;
using FileAnalisys.API.Models;
using FileAnalysis.API.Models;
using FileAnalysis.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace FileAnalysis.API.Controllers
{
    [ApiController]
    [Route("api/scanner")]
    public class ScannerController : Controller
    {
        private readonly IScannerService _scannerService;
        private readonly IMapper _automapper;

        public ScannerController(IScannerService scannerService, IMapper automapper)
        {
            _scannerService = scannerService;
            _automapper = automapper;
        }

        // Checking file for viruses
        // api/process
        [HttpPost()]
        [SwaggerOperation("Check file for viruses")]
        [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
        public ActionResult<ScanResponse> ScanFile(string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new FormatException($"url: '{url}' is not valid.");

            var outputs = _automapper.Map<ScanResponse>(_scannerService.ScanFile(url));
            return Ok(outputs);
        }

    }
}
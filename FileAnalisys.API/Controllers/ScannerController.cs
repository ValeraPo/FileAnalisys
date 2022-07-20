using AutoMapper;
using FileAnalisys.API.Models;
using FileAnalysis.API.Models;
using FileAnalysis.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        [ProducesResponseType(typeof(ScanResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status504GatewayTimeout)]
        [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status500InternalServerError)]
        public ActionResult<ScanResponse> ScanFile(string url)
        {
            // Validate URL. If URL is not correct, I will not pass it to BLL
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new FormatException($"url: '{url}' is not valid.");

            var outputs = _automapper.Map<ScanResponse>(_scannerService.ScanFile(url));
            return Ok(outputs);
        }

    }
}
using FileAnalysis.API.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FileAnalysis.API.Controllers
{
    [ApiController]
    [Route("api/scanner")]
    public class ScannerController : ControllerBase
    {

        // Checking file for viruses
        // api/process
        [HttpPost()]
        [SwaggerOperation("Check file for viruses")]
        public ActionResult<ScanResponse> ScanFile(string url)
        {
            var outputs = "pass";
            return Ok(outputs);
        }


    }
}
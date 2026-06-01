using DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoversController : ControllerBase
    {
        private readonly ICoverService _coverService;
        private readonly IWebHostEnvironment _env;

        public CoversController(ICoverService coverService, IWebHostEnvironment env)
        {
            _coverService = coverService;
            _env = env;
        }

        // POST api/covers/generate
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateCoverResponseDTO>> Generate([FromBody] GenerateCoverRequestDTO request)
        {
            if (request is null
                || string.IsNullOrWhiteSpace(request.ImageBase64)
                || string.IsNullOrWhiteSpace(request.ProductName))
            {
                return BadRequest("ImageBase64 and ProductName are required.");
            }

            GenerateCoverResponseDTO result = await _coverService.GenerateCoversAsync(request, _env.WebRootPath);
            return Ok(result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using DTOs;
using Services;

namespace WebApiShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost]
        public async Task<ActionResult<SearchResponse>> Post([FromBody] SearchQuery request)
        {
            try
            {
                var result = await _searchService.SearchAsync(request, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, ex.Message);
            }
        }
    }
}

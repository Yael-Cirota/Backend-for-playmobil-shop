using DTOs;
using DTOs.Chat;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatController(IProductService productService, IHttpClientFactory httpClientFactory)
        {
            _productService = productService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponseDTO>> Post([FromBody] ChatRequestDTO? req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
                return BadRequest("Message is required.");

            List<ProductDTO> allProducts = await _productService.GetAllAsync();
            var limitedProducts = allProducts.Take(50).ToList();

            var snapshots = limitedProducts
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    description = p.Description,
                    category = p.CategoryName,
                    inStock = p.IsAvailable,
                    imageUrl = p.ImageUrl
                })
                .ToList();

            var payload = new
            {
                message = req.Message,
                history = req.History ?? new List<HistoryItemDTO>(),
                products = snapshots
            };

            var http = _httpClientFactory.CreateClient("AiService");
            var response = await http.PostAsJsonAsync("chat", payload, HttpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
                return StatusCode((int)response.StatusCode, string.IsNullOrWhiteSpace(errorBody) ? "AI service returned an error." : errorBody);
            }

            var rawJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: HttpContext.RequestAborted);

            var reply = rawJson.TryGetProperty("reply", out var replyProp) ? replyProp.GetString() ?? "" : "";

            var productCards = new List<ProductCardDTO>();
            if (rawJson.TryGetProperty("recommended_ids", out var idsProp) && idsProp.ValueKind == JsonValueKind.Array)
            {
                var productLookup = limitedProducts.ToDictionary(p => p.Id);
                foreach (var idEl in idsProp.EnumerateArray())
                {
                    if (idEl.TryGetInt32(out int pid) && productLookup.TryGetValue(pid, out var prod))
                    {
                        productCards.Add(new ProductCardDTO(prod.Id, prod.Name, prod.ImageUrl, prod.Price));
                    }
                }
            }

            return Ok(new ChatResponseDTO(reply, productCards));
        }
    }
}

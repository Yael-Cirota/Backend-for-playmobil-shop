using DTOs;
using System.Net.Http.Json;

namespace Services
{
    public class SearchService : ISearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProductService _products;

        public SearchService(IHttpClientFactory httpClientFactory, IProductService products)
        {
            _httpClientFactory = httpClientFactory;
            _products = products;
        }

        public async Task<SearchResponse> SearchAsync(SearchQuery request, CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException("Query is required.", nameof(request));
            }

            var products = await _products.GetAllAsync();
            var mappedProducts = products
                .Take(30)
                .Select(p => new
                {
                    name = p.Name,
                    price = p.Price,
                    description = p.Description,
                    category = p.CategoryName,
                    inStock = p.IsAvailable
                })
                .ToList();

            var payload = new
            {
                query = request.Query,
                products = mappedProducts,
                top_k = 5
            };

            var http = _httpClientFactory.CreateClient();
            var response = await http.PostAsJsonAsync("http://localhost:8010/search", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(errorBody) ? "Search service returned an error." : errorBody
                );
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException("Invalid response from search service.");
            }

            return result;
        }
    }
}

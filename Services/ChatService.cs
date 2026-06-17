using DTOs.Chat;
using System.Net.Http.Json;

namespace Services
{
    public class ChatService : IChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ChatResponseDTO> SendAsync(ChatRequestDTO req, CancellationToken cancellationToken)
        {
            var http = _httpClientFactory.CreateClient("AiService");

            req = req with
            {
                History = req.History ?? new List<HistoryItemDTO>(),
                Products = req.Products ?? new List<ProductSnapshotDTO>()
            };

            var res = await http.PostAsJsonAsync("chat", req, cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                var upstreamBody = await res.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(upstreamBody) ? "AI service returned an error." : upstreamBody);
            }

            var data = await res.Content.ReadFromJsonAsync<ChatResponseDTO>(cancellationToken: cancellationToken);

            if (data is null)
                throw new InvalidOperationException("AI service returned an empty response.");

            return data;
        }
    }
}

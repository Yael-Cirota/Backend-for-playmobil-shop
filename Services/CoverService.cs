using DTOs;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace Services
{
    public class CoverService : ICoverService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiToken;

        private static readonly string[] PromptTemplates = new[]
        {
            "Two images are provided: Image 1 is a reference face portrait, and Image 2 is a Playmobil toy box cover. Output a single image that is the Playmobil box cover from Image 2 with one change: the face of the main Playmobil figure on the cover is replaced with a Playmobil-style illustrated version of the face from Image 1, rendered in the same toy art style as the rest of the box. All other elements of the box—background, props, branding, text, logos, and colors—must remain exactly as in the original.",
            "Given two images: a portrait photo (Image A) and a Playmobil toy box cover (Image B), produce a single output image that is Image B with the face of its primary Playmobil character redesigned to resemble the person in Image A, drawn in the Playmobil toy illustration style. The output must look like an official Playmobil box cover. Preserve all box art, background, props, logos, and text exactly as they appear in Image B.",
            "You are given a portrait photo and a Playmobil toy box cover. Create a single output image: the same Playmobil box cover, but with the main Playmobil character's face reimagined in the Playmobil toy art style to resemble the face from the portrait photo. The illustration style must match the existing box art. Every other element on the box—scene, background, accessories, text, and branding—must be preserved unchanged from the original box cover."
        };

        public CoverService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiToken = configuration["OpenAI:ApiToken"]?.Trim()
                ?? throw new InvalidOperationException("OpenAI:ApiToken is not configured.");

            if (string.IsNullOrWhiteSpace(_apiToken))
                throw new InvalidOperationException("OpenAI:ApiToken is empty. Please set it in appsettings.json or user secrets.");
        }

        public async Task<GenerateCoverResponseDTO> GenerateCoversAsync(GenerateCoverRequestDTO request, string webRootPath)
        {
            string rawBase64 = request.ImageBase64.Contains(',')
                ? request.ImageBase64.Split(',')[1]
                : request.ImageBase64;

            if (!TryDecodeImageBase64(rawBase64, out byte[] childBytes))
                throw new InvalidOperationException("ImageBase64 from client is not a valid base64 image payload.");

            var boxUri = new Uri(request.BoxImageUrl);
            string relativePath = boxUri.AbsolutePath.TrimStart('/');
            string boxFilePath = Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            byte[] boxBytes = await File.ReadAllBytesAsync(boxFilePath);

            var dataUris = new List<string>();
            foreach (var template in PromptTemplates)
            {
                byte[] imageBytes = await GenerateImageBytesAsync(childBytes, boxBytes, template);
                string b64 = Convert.ToBase64String(imageBytes);
                dataUris.Add($"data:image/png;base64,{b64}");
                await Task.Delay(500);
            }

            return new GenerateCoverResponseDTO(dataUris.ToArray());
        }

        private async Task<byte[]> GenerateImageBytesAsync(byte[] childBytes, byte[] boxBytes, string prompt)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            byte[] childPng = await ToPngAsync(childBytes);
            byte[] boxPng = await ToPngAsync(boxBytes);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("gpt-image-1"), "model");
            form.Add(new StringContent(prompt), "prompt");
            form.Add(new StringContent("1"), "n");
            form.Add(new StringContent("1024x1024"), "size");

            var childContent = new ByteArrayContent(childPng);
            childContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(childContent, "image[]", "child.png");

            var boxContent = new ByteArrayContent(boxPng);
            boxContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(boxContent, "image[]", "box.png");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/edits");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            httpRequest.Content = form;

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(httpRequest);
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx
                && socketEx.SocketErrorCode == SocketError.AccessDenied)
            {
                throw new InvalidOperationException(
                    "Cannot connect to api.openai.com:443 (SocketError 10013 / AccessDenied). " +
                    "This is usually a local firewall, antivirus web shield, VPN, or corporate network policy block.",
                    ex);
            }
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"OpenAI {(int)response.StatusCode}: {errorBody}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data")[0];

            if (data.TryGetProperty("b64_json", out var b64Prop) && b64Prop.ValueKind == JsonValueKind.String)
            {
                string b64 = b64Prop.GetString()!;
                if (TryDecodeImageBase64(b64, out byte[] decodedBytes))
                    return decodedBytes;

                if (Uri.TryCreate(b64, UriKind.Absolute, out var embeddedUrl)
                    && (embeddedUrl.Scheme == Uri.UriSchemeHttp || embeddedUrl.Scheme == Uri.UriSchemeHttps))
                {
                    using var downloadClient = _httpClientFactory.CreateClient();
                    return await downloadClient.GetByteArrayAsync(embeddedUrl);
                }
            }

            if (data.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
            {
                string imageUrl = urlProp.GetString()!;
                using var downloadClient = _httpClientFactory.CreateClient();
                return await downloadClient.GetByteArrayAsync(imageUrl);
            }

            throw new InvalidOperationException($"OpenAI response had neither b64_json nor url. Response: {json}");
        }

        private static bool TryDecodeImageBase64(string value, out byte[] decoded)
        {
            decoded = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(value)) return false;

            string normalized = value.Trim();

            int commaIndex = normalized.IndexOf(',');
            if (commaIndex >= 0)
                normalized = normalized[(commaIndex + 1)..];

            normalized = normalized
                .Trim()
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty)
                .Replace('-', '+')
                .Replace('_', '/');

            int mod4 = normalized.Length % 4;
            if (mod4 == 1) return false;
            if (mod4 == 2) normalized += "==";
            if (mod4 == 3) normalized += "=";

            try
            {
                decoded = Convert.FromBase64String(normalized);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static async Task<byte[]> ToPngAsync(byte[] sourceBytes)
        {
            using var image = Image.Load(sourceBytes);
            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new PngEncoder());
            return ms.ToArray();
        }
    }
}

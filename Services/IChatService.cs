using DTOs.Chat;

namespace Services
{
    public interface IChatService
    {
        Task<ChatResponseDTO> SendAsync(ChatRequestDTO req, CancellationToken cancellationToken);
    }
}

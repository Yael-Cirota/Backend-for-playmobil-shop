using DTOs;

namespace Services
{
    public interface ICoverService
    {
        Task<GenerateCoverResponseDTO> GenerateCoversAsync(GenerateCoverRequestDTO request, string webRootPath);
    }
}

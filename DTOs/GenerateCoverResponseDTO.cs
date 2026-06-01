namespace DTOs
{
    public record GenerateCoverResponseDTO(
        IEnumerable<string> CoverUrls
    );
}

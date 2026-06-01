namespace DTOs
{
    public record GenerateCoverRequestDTO(
        string ImageBase64,
        int OrderId,
        int ProductId,
        string ProductName,
        string BoxImageUrl
    );
}

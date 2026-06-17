namespace DTOs.Chat
{
    public record ProductCardDTO(
        int Id,
        string Name,
        string ImageUrl,
        double Price
    );

    public record ChatResponseDTO(
        string Reply,
        List<ProductCardDTO> Products
    );
}

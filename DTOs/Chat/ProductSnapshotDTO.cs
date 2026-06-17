namespace DTOs.Chat
{
    public record ProductSnapshotDTO(
        int Id,
        string Name,
        string Category,
        double Price,
        bool IsAvailable,
        string Description,
        string ImageUrl
    );
}

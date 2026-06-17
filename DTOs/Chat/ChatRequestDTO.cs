namespace DTOs.Chat
{
    public record ChatRequestDTO(
        string Message,
        List<HistoryItemDTO>? History = null,
        List<ProductSnapshotDTO>? Products = null
    );
}

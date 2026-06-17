using DTOs;

namespace Services
{
    public interface ISearchService
    {
        Task<SearchResponse> SearchAsync(SearchQuery request, CancellationToken cancellationToken);
    }
}

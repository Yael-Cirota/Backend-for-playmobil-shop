using DTOs;
using Entities;

namespace Services
{
    public interface IProductService
    {
        public Task<List<ProductDTO>> GetAllAsync();
        public Task<List<ProductDTO>> GetProducts(int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice);
        public Task<ProductDTO> GetProductById(int id);
        public Task<ProductDTO> AddProduct(PostProductDTO product);
        public Task UpdateProduct(int id, PostProductDTO product);
    }
}
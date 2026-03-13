using System.Collections.Generic;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface IProductManagementService
    {
        List<Category> GetCategories();
        List<ProductListItem> GetProducts();
        void AddProduct(string name, int categoryId, decimal price, bool isActive);
        void UpdateProduct(int productId, string name, int categoryId, decimal price, bool isActive);
        void MarkProductInactive(int productId);
        void DeleteProduct(int productId);
    }
}

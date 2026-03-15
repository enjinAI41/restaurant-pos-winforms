using System.Collections.Generic;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface IProductManagementService
    {
        List<Category> GetCategories(bool onlyActive = true);
        void AddCategory(string name, bool isActive);
        void UpdateCategory(int categoryId, string name, bool isActive);
        void SetCategoryActive(int categoryId, bool isActive);
        List<ProductListItem> GetProducts();
        void AddProduct(string name, int categoryId, decimal price, bool isActive);
        void UpdateProduct(int productId, string name, int categoryId, decimal price, bool isActive);
        void MarkProductInactive(int productId);
        void DeleteProduct(int productId);
    }
}

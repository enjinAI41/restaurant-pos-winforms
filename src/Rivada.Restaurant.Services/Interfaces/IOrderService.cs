using System.Collections.Generic;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface IOrderService
    {
        List<Category> GetCategories();
        List<Product> GetProductsByCategory(int categoryId);
        BillDetails GetBillDetails(int billId);
        void AddProductToBill(int billId, int productId, string? note = null);
        void IncreaseBillItemQuantity(int billItemId);
        void DecreaseBillItemQuantity(int billItemId);
        void RemoveBillItem(int billItemId);
        void UpdateBillItemNote(int billItemId, string? note);
    }
}

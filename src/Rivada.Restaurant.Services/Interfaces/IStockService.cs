using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Interfaces;

public interface IStockService
{
    List<StockItem> GetStock();
    List<StockMovement> GetHistory(int productId);
    void Configure(int productId, bool tracked, int minimum, long revision, int userId);
    void Adjust(int productId, string kind, int quantity, string note, long revision, int userId);
}

using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.Services;

public sealed class StockService : IStockService
{
    private readonly StockRepository _repository;
    public StockService(StockRepository repository) => _repository = repository;
    public List<StockItem> GetStock() => _repository.GetStock();
    public List<StockMovement> GetHistory(int productId) => _repository.GetHistory(productId);
    public void Configure(int productId, bool tracked, int minimum, long revision, int userId)
        => _repository.Configure(productId, tracked, minimum, revision, userId);
    public void Adjust(int productId, string kind, int quantity, string note, long revision, int userId)
        => _repository.Adjust(productId, kind, quantity, note, revision, userId);
}

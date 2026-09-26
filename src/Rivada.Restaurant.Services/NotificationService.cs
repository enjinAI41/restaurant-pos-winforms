using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.Services;

public sealed class NotificationService : INotificationService
{
    private readonly NotificationRepository _repository;
    public NotificationService(NotificationRepository repository) => _repository = repository;
    public NotificationFeed GetFeed() => _repository.GetFeed();
}

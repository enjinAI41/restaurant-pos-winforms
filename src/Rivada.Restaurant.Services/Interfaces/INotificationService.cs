using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Interfaces;

public interface INotificationService
{
    NotificationFeed GetFeed();
}

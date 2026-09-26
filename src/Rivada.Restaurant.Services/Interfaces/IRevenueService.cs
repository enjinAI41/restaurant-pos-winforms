using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Interfaces;

public interface IRevenueService
{
    int BusinessDayStartHour { get; }
    RevenueReport GetReport(DateTime startDate,DateTime endDate,int userId);
}

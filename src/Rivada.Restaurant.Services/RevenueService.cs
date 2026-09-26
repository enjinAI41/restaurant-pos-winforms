using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.Services;

public sealed class RevenueService : IRevenueService
{
    private readonly RevenueRepository _repository;
    private readonly TimeZoneInfo _timeZone;
    public int BusinessDayStartHour { get; }

    public RevenueService(RevenueRepository repository,TimeZoneInfo? timeZone=null,int businessDayStartHour=0)
    {
        if(businessDayStartHour<0 || businessDayStartHour>23) throw new ArgumentOutOfRangeException(nameof(businessDayStartHour));
        _repository=repository; _timeZone=timeZone??TimeZoneInfo.Local;
        BusinessDayStartHour=businessDayStartHour;
    }

    public RevenueReport GetReport(DateTime startDate,DateTime endDate,int userId)
    {
        var start=startDate.Date; var end=endDate.Date;
        if(end<start) throw new InvalidOperationException("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        if(end.Year>=9999) throw new InvalidOperationException("Geçerli bir bitiş tarihi seçin.");
        var localStart=DateTime.SpecifyKind(start.AddHours(BusinessDayStartHour),DateTimeKind.Unspecified);
        var localEnd=DateTime.SpecifyKind(end.AddDays(1).AddHours(BusinessDayStartHour),DateTimeKind.Unspecified);
        if(_timeZone.IsInvalidTime(localStart) || _timeZone.IsInvalidTime(localEnd))
            throw new InvalidOperationException("Seçilen tarih saat değişimine denk geliyor. Başka bir tarih aralığı seçin.");
        var report=_repository.Read(TimeZoneInfo.ConvertTimeToUtc(localStart,_timeZone),TimeZoneInfo.ConvertTimeToUtc(localEnd,_timeZone),userId);
        report.StartDate=start; report.EndDate=end;
        report.RefreshedAt=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,_timeZone);
        foreach(var payment in report.Payments) payment.PaidAt=TimeZoneInfo.ConvertTimeFromUtc(payment.PaidAt,_timeZone);
        report.Days=report.Payments.GroupBy(x=>x.PaidAt.AddHours(-BusinessDayStartHour).Date)
            .Select(x=>new DailyRevenue { Date=x.Key,PaymentCount=x.Count(),
                Cash=x.Where(p=>p.PaymentType==PaymentType.Cash).Sum(p=>p.Amount),
                Card=x.Where(p=>p.PaymentType==PaymentType.Card).Sum(p=>p.Amount) })
            .OrderByDescending(x=>x.Date).ToList();
        return report;
    }
}

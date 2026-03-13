using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface IPaymentService
    {
        PaymentViewData GetPaymentViewData(int billId, string tableName);
        PaymentCompletionResult CompletePayment(int billId, int paidByUserId, PaymentType paymentType);
    }
}

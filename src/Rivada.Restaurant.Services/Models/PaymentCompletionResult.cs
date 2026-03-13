namespace Rivada.Restaurant.Services.Models
{
    public class PaymentCompletionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

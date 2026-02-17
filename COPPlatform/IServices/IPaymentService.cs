using COPPlatform.Common.Models;
using COPPlatform.Dtos.PaymentDto;

namespace COPPlatform.IServices
{
    public interface IPaymentService
    {
        Task<ResultResponseDto<CheckoutSessionResponse>> CreateCheckoutSession(CreateCheckoutSessionDto request);
        Task<ResultResponseDto<VerifySessionResponse>> VerifySession(VerifySessionDto request);
        Task<ResultResponseDto<string>> StripeWebhook();
    }
}

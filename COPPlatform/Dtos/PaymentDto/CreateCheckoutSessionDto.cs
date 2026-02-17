using COPPlatform.Enums;

namespace COPPlatform.Dtos.PaymentDto
{
    public class CreateCheckoutSessionDto
    {
        public int UserID { get; set; }
        public TieredAccessPlan Tier { get; set; }
        public int Amount { get; set; }
    }
}

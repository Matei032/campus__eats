namespace CampusEats.Backend.Common.Config;


// stripe listen --forward-to localhost:5156/api/payments/webhook/stripe
public class StripeSettings
{
    public string SecretKey { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}
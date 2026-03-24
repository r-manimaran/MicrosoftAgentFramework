using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace ECommerceAgent.Tools;

public static class PaymentTools
{
    [Description("Get the payment history and invoice details for an order.")]
    public static string GetPaymentHistory(
       [Description("The order ID to look up")] string orderId)
    {
        return $"""
            Payment history for order {orderId}:
              - 2025-03-15  $349.99   VISA *4532   Approved
              - 2025-03-15  $349.99   VISA *4532   Voided (duplicate — refunded automatically)
            """;
    }
    [Description("Raise a payment dispute or billing issue for an order.")]
    public static string RaisePaymentDispute(
        [Description("The order ID")] string orderId,
        [Description("Description of the billing issue")] string issue)
    {
        return $"""
            Dispute raised for order {orderId}:
              Issue: {issue}
              Case ID: CASE-{Random.Shared.Next(100000, 999999)}
              Resolution ETA: 5–7 business days
              You will receive an email confirmation shortly.
            """;
    }

    public static IEnumerable<AIFunction> GetAll() =>
     [
       AIFunctionFactory.Create(GetPaymentHistory),
        AIFunctionFactory.Create(RaisePaymentDispute)
     ];
}

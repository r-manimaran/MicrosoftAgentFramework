using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace ECommerceAgent.Tools;

public static class ReturnTools
{
    [Description("Check if an order item is eligible for return or refund.")]
    public static string CheckReturnEligibility([Description("The order ID to check")] string orderId)
    {
        return $"Order {orderId}: Eligible for return within 30-day window. " +
               "Item must be unopened or defective.";

    }

    [Description("Initiate a return request and generate a return shipping label.")]
    public static string InitiateReturn(
        [Description("The order ID to return")]string orderId,
        [Description("Reason for return: defective, wrong_item, changed_mind")] string reason)
    {
        return $"""
            Return initiated for order {orderId}:
              Reason: {reason}
              Return Label: RTN-{orderId}-{DateTime.UtcNow:yyyyMMdd}
              Drop-off: Any UPS location within 14 days
              Refund ETA: 3–5 business days after receipt
            """;
    }

    [Description("Get the status of an existing return request.")]
    public static string GetReturnStatus(
        [Description("The return label number")] string returnLabel)
    {
        return $"Return {returnLabel}: In transit to warehouse. Refund will process on receipt.";
    }

    public static IEnumerable<AIFunction> GetAll() => 
        [
            AIFunctionFactory.Create(CheckReturnEligibility),
            AIFunctionFactory.Create(InitiateReturn),
            AIFunctionFactory.Create(GetReturnStatus)
        ];

}

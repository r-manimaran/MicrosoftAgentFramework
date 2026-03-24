using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace ECommerceAgent.Tools;

public static class ShippingTools
{
    [Description("Track the shipment and get current delivery status for an order.")]
    public static string TrackShipment(
     [Description("The order ID to track")] string orderId)
    {
        return $"""
            Shipment for order {orderId}:
              Carrier: UPS
              Tracking: 1Z999AA10123456784
              Status: Out for delivery
              Last Scan: 2025-03-22 08:14 — Local delivery facility
              ETA: Today by 8:00 PM
            """;
    }

    [Description("Get estimated delivery date for an order.")]
    public static string GetDeliveryEstimate(
        [Description("The order ID")] string orderId)
    {
        return $"Order {orderId} estimated delivery: Today, March 22 by 8:00 PM.";
    }

    public static IEnumerable<AIFunction> GetAll() =>
    [
        AIFunctionFactory.Create(TrackShipment),
        AIFunctionFactory.Create(GetDeliveryEstimate)
    ];
}

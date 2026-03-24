using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceAgent.Tools;

public static class OrderTools
{
    [Description("Get details of a customer order by order ID.")]
    public static string GetOrderDetails(
        [Description("The Order ID, eg. ORD-12345")] string orderId)
    {
        // Similate DB Lookup
        return orderId switch
        {
            "ORD-12345" => """
                Order ORD-12345:
                Status: Shipped
                Items: Sony WH-1000XM5 Headphones (x1) — $349.99
                Placed: 2025-03-15
                Estimated Delivery: 2025-03-22
            """,
            _ => $"Order {orderId} not found. Please verify the order ID."
        };
    }
    [Description("Get the full order history for a customer account.")]
    public static string GetOrderHistory(
        [Description("The customer account ID")] string customerId)
    {
        return $"""
            Order history for customer {customerId}:
            Order history for customer {customerId}:
            - ORD-12345  (2025-03-15) — Shipped — $349.99
            - ORD-11982  (2025-02-28) — Delivered — $79.99
            - ORD-10445  (2025-01-10) — Delivered — $129.00
            """;
    }

    // Factory: convert to AIFunction list
    public static IEnumerable<AIFunction> GetAll() =>
        [
            AIFunctionFactory.Create(GetOrderDetails),
            AIFunctionFactory.Create(GetOrderHistory)
        ];
}

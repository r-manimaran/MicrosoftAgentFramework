using MCPapp.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCPapp.Server.Tools;

/// <summary>
/// Order management tools.
/// create_order and cancel_order are write tools → stripped for readonly sessions.
/// </summary>
/// <summary>
/// Order management tools.
/// create_order and cancel_order are write tools → stripped for readonly sessions.
/// </summary>
[McpServerToolType]
public sealed class OrderTool(OrderService orders)
{
    [McpServerTool(Name = "get_order")]
    [Description("Retrieve an order by its ID.")]
    public OrderDto? GetOrder(
        [Description("Order ID, e.g. 'ORD-1001'.")] string orderId)
    {
        var o = orders.GetOrder(orderId);
        return o is null ? null : ToDto(o);
    }

    [McpServerTool(Name = "list_orders")]
    [Description("List all orders in the system.")]
    public List<OrderDto> ListOrders() =>
        orders.GetAllOrders().Select(ToDto).ToList();

    // ── Write tools ───────────────────────────────────────────────────────────

    [McpServerTool(Name = "create_order")]
    [Description("Place a new order for a product. Deducts from available stock.")]
    public OrderDto CreateOrder(
        [Description("Product ID to order.")] string productId,
        [Description("Number of units to order.")] int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1.");

        var order = orders.CreateOrder(productId, quantity);
        return ToDto(order);
    }

    [McpServerTool(Name = "cancel_order")]
    [Description("Cancel an existing order and restore the stock.")]
    public CancelResult CancelOrder(
        [Description("Order ID to cancel.")] string orderId)
    {
        var ok = orders.CancelOrder(orderId);
        return ok
            ? new CancelResult(true, $"Order {orderId} cancelled. Stock restored.")
            : new CancelResult(false, $"Order {orderId} not found or already cancelled.");
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.ProductId, o.ProductName, o.Quantity,
        o.TotalPrice, o.Status, o.CreatedAt);
}

public record OrderDto(
    string Id,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);

public record CancelResult(bool Success, string Message);


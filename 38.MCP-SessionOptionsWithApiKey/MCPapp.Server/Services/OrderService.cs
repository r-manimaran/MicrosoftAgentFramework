namespace MCPapp.Server.Services;

public record Order(
    string Id,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);

public sealed class OrderService(ProductCatalogService catalog)
{
    private readonly List<Order> _orders = [];
    private int _nextId = 1000;

    public Order CreateOrder(string productId, int quantity)
    {
        var product = catalog.GetById(productId) 
                    ?? throw new ArgumentException($"Product '{productId}' not found.");

        if(product.StockQty < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock: requested {quantity}, available {product.StockQty}.");
        catalog.UpdateStock(productId, product.StockQty - quantity);

        var order = new Order(
          Id: $"ORD-{++_nextId}",
          ProductId: productId,
          ProductName: product.Name,
          Quantity: quantity,
          TotalPrice: product.Price * quantity,
          Status: "Confirmed",
          CreatedAt: DateTime.UtcNow);

        _orders.Add(order);
        return order;
    }

    public Order? GetOrder(string orderId)
        => _orders.FirstOrDefault(o => o.Id.Equals(orderId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Order> GetAllOrders() => _orders.AsReadOnly();

    public bool CancelOrder(string orderId)
    {
        var idx = _orders.FindIndex(o => o.Id.Equals(orderId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;

        var order = _orders[idx];
        if (order.Status == "Cancelled") return false;

        // Restore stock
        catalog.UpdateStock(order.ProductId,
            (catalog.GetById(order.ProductId)?.StockQty ?? 0) + order.Quantity);

        _orders[idx] = order with { Status = "Cancelled" };
        return true;
    }
}

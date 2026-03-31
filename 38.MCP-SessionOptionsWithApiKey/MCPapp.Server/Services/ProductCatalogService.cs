namespace MCPapp.Server.Services;

public record Product(
    string Id,
    string Name,
    string Category,
    decimal Price,
    int StockQty);

public sealed class ProductCatalogService
{
    private readonly List<Product> _products = 
        [
            new("P001", "Laptop Pro 15",      "Electronics",  1299.99m, 42),
            new("P002", "Wireless Mouse",     "Electronics",    29.99m, 150),
            new("P003", "USB-C Hub 7-port",   "Electronics",    49.99m, 88),
            new("P004", "Standing Desk",      "Furniture",     499.00m, 17),
            new("P005", "Ergonomic Chair",    "Furniture",     349.00m, 9),
            new("P006", "Noise-Cancel Headphones", "Electronics", 199.99m, 63),
            new("P007", "Webcam 4K",          "Electronics",    89.99m, 31),
            new("P008", "Monitor 27\" 4K",    "Electronics",   599.00m, 22),
        ];


    public IReadOnlyList<Product> GetAll() => _products.AsReadOnly();

    public IReadOnlyList<Product> Search(string? category =null, decimal? maxPrice =null) =>
        _products.Where(p =>
            (string.IsNullOrEmpty(category) || p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) &&
            (!maxPrice.HasValue || p.Price <= maxPrice.Value))
        .ToList();

    public Product? GetById(string id) =>
        _products.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public bool UpdateStock(string id, int newQty)
    {
        var idx = _products.FindIndex(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;
        _products[idx] = _products[idx] with { StockQty = newQty };
        return true;
    }
}

using MCPapp.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCPapp.Server.Tools;

/// <summary>
/// Product catalog tools - read (search /get) and write(update stock)
/// Write tools are stripped from readonly sessions by ConfigureSessionOptions
/// </summary>
[McpServerToolType]
public sealed class ProductTools(ProductCatalogService catalog)
{
    [McpServerTool(Name = "search_products")]
    [Description("Search the product catalog. Optionally filter by category and/or maximum price.")]
    public List<ProductSummary> SearchProducts(
       [Description("Category to filter by, e.g. 'Electronics' or 'Furniture'. Leave empty for all.")] string? category = null,
       [Description("Maximum price in USD.")] decimal? maxPrice = null)
    {
        return catalog.Search(category, maxPrice)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Category, p.Price, p.StockQty))
            .ToList();
    }

    [McpServerTool(Name = "get_product")]
    [Description("Get full details for a single product by its ID.")]
    public ProductSummary? GetProduct(
        [Description("Product ID, e.g. 'P001'.")] string productId)
    {
        var p = catalog.GetById(productId);
        return p is null ? null : new ProductSummary(p.Id, p.Name, p.Category, p.Price, p.StockQty);
    }

    // ── Write tool — stripped for readonly sessions ───────────────────────────
    [McpServerTool(Name = "update_stock")]
    [Description("Update the stock quantity for a product. Requires admin role.")]
    public UpdateStockResult UpdateStock(
        [Description("Product ID.")] string productId,
        [Description("New stock quantity (must be >= 0).")] int newQuantity)
    {
        if (newQuantity < 0)
            return new UpdateStockResult(false, "Quantity cannot be negative.");

        var ok = catalog.UpdateStock(productId, newQuantity);
        return ok
            ? new UpdateStockResult(true, $"Stock for {productId} updated to {newQuantity}.")
            : new UpdateStockResult(false, $"Product '{productId}' not found.");
    }
}
public record ProductSummary(string Id, string Name, string Category, decimal Price, int StockQty);
public record UpdateStockResult(bool Success, string Message);

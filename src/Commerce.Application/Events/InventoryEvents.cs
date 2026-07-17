namespace Commerce.Application.Events;

public sealed record StockLow(Guid ProductId, string ProductName, string Brand, int CurrentStock, int ReorderPoint);

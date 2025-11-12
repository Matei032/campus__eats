using System.Collections.Concurrent;

namespace CampusEats.Frontend.Services;

public class CartService
{
    private readonly ConcurrentDictionary<Guid, CartItem> _items = new();

    public IEnumerable<CartItem> Items => _items.Values;
    public decimal Total => _items.Values.Sum(i => i.Subtotal);

    public void Add(ProductDto p, int qty = 1)
    {
        _items.AddOrUpdate(p.Id,
            _ => new CartItem(p.Id, p.Name, p.Price, qty),
            (_, old) => old with { Quantity = old.Quantity + qty });
    }

    public void UpdateQty(Guid productId, int qty)
    {
        if (_items.TryGetValue(productId, out var old))
        {
            if (qty <= 0) _items.TryRemove(productId, out _);
            else _items[productId] = old with { Quantity = qty };
        }
    }

    public void Remove(Guid productId) => _items.TryRemove(productId, out _);
    public void Clear() => _items.Clear();
}

public record CartItem(Guid ProductId, string Name, decimal UnitPrice, int Quantity)
{
    public decimal Subtotal => UnitPrice * Quantity;
}
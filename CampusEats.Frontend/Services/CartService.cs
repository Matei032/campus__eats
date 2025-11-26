using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusEats.Frontend.Models;

namespace CampusEats.Frontend.Services
{
    public class CartService
    {
        private readonly LocalStorageService _localStorage;
        // Folosim o listă internă pentru manipulare ușoară
        private List<CartItem> _cart = new();

        // Eveniment la care se abonează componentele (ex: HeaderBar, Cart.razor)
        public event Action? OnChange;

        public CartService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // Proprietăți publice
        public IReadOnlyList<CartItem> Items => _cart;
        public int Count => _cart.Sum(i => i.Quantity);
        public decimal Total => _cart.Sum(i => i.Subtotal);

        // --- Inițializare ---
        // Această metodă trebuie apelată în App.razor sau MainLayout.razor
        public async Task InitializeAsync()
        {
            // Citim coșul salvat anterior (dacă există)
            var savedCart = await _localStorage.GetItemAsync<List<CartItem>>("cart");
            if (savedCart != null)
            {
                _cart = savedCart;
                NotifyStateChanged();
            }
        }

        // --- Operații ---

        public async Task AddToCart(ProductDto product, int quantity = 1)
        {
            var existingItem = _cart.FirstOrDefault(i => i.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _cart.Add(new CartItem(product.Id, product.Name, product.Price, quantity));
            }

            await SaveCart();
            NotifyStateChanged();
        }

        public async Task UpdateQty(Guid productId, int quantity)
        {
            var item = _cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    _cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                await SaveCart();
                NotifyStateChanged();
            }
        }

        public async Task Remove(Guid productId)
        {
            var item = _cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                _cart.Remove(item);
                await SaveCart();
                NotifyStateChanged();
            }
        }

        public async Task Clear()
        {
            _cart.Clear();
            await _localStorage.RemoveItemAsync("cart");
            NotifyStateChanged();
        }

        // --- Helper Privați ---

        private async Task SaveCart()
        {
            await _localStorage.SetItemAsync("cart", _cart);
        }
        public async Task UpdateProductDetails(Guid productId, string name, decimal currentPrice)
        {
            var item = _cart.FirstOrDefault(i => i.ProductId == productId);
    
            if (item != null)
            {
                bool changed = false;

                if (item.UnitPrice != currentPrice)
                {
                    item.UnitPrice = currentPrice;
                    changed = true;
                }

                if (item.Name != name)
                {
                    item.Name = name;
                    changed = true;
                }

                if (changed)
                {
                    await SaveCart();
                    NotifyStateChanged();
                }
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
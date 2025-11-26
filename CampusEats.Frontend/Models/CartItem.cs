using System;

namespace CampusEats.Frontend.Models
{
    public class CartItem
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Constructor implicit necesar pentru deserializare JSON
        public CartItem() { }

        public CartItem(Guid productId, string name, decimal unitPrice, int quantity)
        {
            ProductId = productId;
            Name = name;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}
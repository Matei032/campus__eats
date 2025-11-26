using System;
using System.Collections.Generic;

namespace CampusEats.Frontend.Models.Requests
{
    public class CreateOrderCommand
    {
        public Guid UserId { get; set; }
        // Backend validator permite: "Card", "Cash", "Loyalty" sau null
        public string? PaymentMethod { get; set; } 
        public string? Notes { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
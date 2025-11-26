using System;

namespace CampusEats.Frontend.Models.Requests
{
    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        
        // IMPORTANT: Backend-ul se așteaptă la "SpecialInstructions", nu la "Notes"
        public string? SpecialInstructions { get; set; } 
    }
}
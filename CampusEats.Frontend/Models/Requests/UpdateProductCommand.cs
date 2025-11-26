using System;
using System.Collections.Generic;

namespace CampusEats.Frontend.Models.Requests
{
    public class UpdateProductCommand
    {
        public Guid Id { get; set; } // Backend-ul cere ID-ul și în body
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string> Allergens { get; set; } = new();
        public string? DietaryRestrictions { get; set; }
        public bool IsAvailable { get; set; }
    }
}
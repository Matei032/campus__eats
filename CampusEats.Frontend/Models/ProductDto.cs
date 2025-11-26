using System;
using System.Collections.Generic;

namespace CampusEats.Frontend.Models
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        
        // --- LISTA NOUĂ DE PROPRIETĂȚI ---
        // Trebuie să fie compatibile cu JSON-ul primit
        public List<string> Allergens { get; set; } = new();
        public string? DietaryRestrictions { get; set; } // Poate fi null în JSON
        
        public DateTime CreatedAt { get; set; }
        
        // CRITIC: Adaugă '?' pentru că în JSON vine "updatedAt": null
        public DateTime? UpdatedAt { get; set; } 
    }
}
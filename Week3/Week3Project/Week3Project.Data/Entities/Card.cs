using System.ComponentModel.DataAnnotations;

namespace Week3Project.Data.Entities;

public class Card
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Sku { get; set; } = string.Empty; // e.g., MTG-MH3-011-F
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., Ragavan, Nimble Pilferer
    
    public decimal Price { get; set; }

    // Navigation property (1:1 to Inventory)
    public CardInventory? Inventory { get; set; }
}
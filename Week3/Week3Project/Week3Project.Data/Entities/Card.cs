using System.ComponentModel.DataAnnotations;

namespace Week3Project.Data.Entities;

public class Card
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Sku { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }

    // Navigation property (1:1 to Inventory)
    public CardInventory? Inventory { get; set; }
}
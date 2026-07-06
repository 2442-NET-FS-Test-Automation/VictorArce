using System.ComponentModel.DataAnnotations;

namespace Week3Project.Data.Entities;

public class Customer
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    // Navigation property
    public List<PurchaseOrder> Orders { get; set; } = new List<PurchaseOrder>();
}
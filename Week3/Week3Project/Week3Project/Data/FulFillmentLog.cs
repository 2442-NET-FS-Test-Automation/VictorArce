using System.ComponentModel.DataAnnotations;
namespace Week3Project.Data;

public class FulFillmentLog
{
    public int Id { get; set; }
    
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // e.g., "Success", "ConflictRetry", "Backorder"
    
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
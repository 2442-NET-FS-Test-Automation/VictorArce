using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Week3Project.Data.Entities;

public class FulFillmentLog
{
    public int Id { get; set; }
    
    public int PurchaseOrderId { get; set; }
    
    [StringLength(50)]
    public string Type { get; set; } = default!; // e.g., "Success", "ConflictRetry", "Backorder"
    
    //This is that field where it says what happened to the order
    [StringLength(500)]
    public string Message { get; set; } = default!;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
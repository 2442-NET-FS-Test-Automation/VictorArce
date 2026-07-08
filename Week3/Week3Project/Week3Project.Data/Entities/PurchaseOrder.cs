using System.ComponentModel.DataAnnotations.Schema;
using Week3Project.Data.Enum;

namespace Week3Project.Data.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    
    //Foreign key from our Customer table
    public int CustomerId { get; set; }
    
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = default!;

    public OrderPriority Priority { get; set; }
    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Even with the single-line simplification, keeping an Order -> OrderLine breakdown
    // protects the 3NF schema design for future expansions.
    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
    public ICollection<FulFillmentLog> FulfillmentLogs { get; set; } = new List<FulFillmentLog>();
}
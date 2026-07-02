namespace Week3Project.Data;

public class PurchaseOrder
{
    public int Id { get; set; }
    
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    public OrderPriority Priority { get; set; }
    public OrderStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Even with the single-line simplification, keeping an Order -> OrderLine breakdown
    // protects your 3NF schema design for future expansions.
    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
    public ICollection<FulfillmentLog> FulfillmentLogs { get; set; } = new List<FulfillmentLog>();
}
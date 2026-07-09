using Week3Project.Data.Enum;

namespace Week3Project.Data.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    
    //Foreign key from our Customer table
    public int CustomerId { get; set; }
    
    public Customer Customer { get; set; } = default!;

    public OrderPriority Priority { get; set; }
    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public List<OrderLine> OrderLines { get; set; } = new();
}
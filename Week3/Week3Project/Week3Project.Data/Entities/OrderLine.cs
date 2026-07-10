using System.ComponentModel.DataAnnotations.Schema;

namespace Week3Project.Data.Entities;

public class OrderLine
{
    public int Id { get; set; }
    
    public int PurchaseOrderId { get; set; }
    
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    
    public int Quantity { get; set; }
}
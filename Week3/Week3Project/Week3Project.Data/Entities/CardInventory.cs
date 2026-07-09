using System.ComponentModel.DataAnnotations;

namespace Week3Project.Data.Entities;

public class CardInventory
{
    public int Id { get; set; }
    
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    
    public int QuantityOnHand { get; set; }
    
    // Concurrency Token managed automatically by SQL Server
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}
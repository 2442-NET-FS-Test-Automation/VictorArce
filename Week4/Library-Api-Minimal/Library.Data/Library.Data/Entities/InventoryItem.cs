namespace Library.Data.Entities;

public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId{ get; set; }
    public Product Product { get; set; } = default;
    public int CurrentStock { get; set; }
    public byte[] RowVersion { get; set; } = default;
}
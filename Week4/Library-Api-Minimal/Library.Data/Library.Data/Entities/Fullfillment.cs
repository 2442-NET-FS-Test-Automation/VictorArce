namespace Library.Data.Entities;

public class FulfillmentEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; } //FK -> Order
    public string Type { get; set; } = default;
    public DateTime FulfilledAt { get; set; } = DateTime.UtcNow;
}
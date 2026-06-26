namespace Library.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; } //FK -> Customer
    public Customer Customer { get; set; } = default;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime OrderCreated { get; set; } = DateTime.UtcNow;
    public DateTime? OrderCompleted { get; set; }

    //Every Order has one or more OrderLines
    //Orderlines are the actual product and quantity of something
    //on the order
    public List<OrderLines> Lines { get; set; } = new();

    }
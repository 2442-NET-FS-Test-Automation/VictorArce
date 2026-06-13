namespace Inventory.Classes;

public abstract class StockItems
{
    //Private properties
    private static int _newId = 1;
    
    //Public properties
    public int Id { get; }
    public int Quantity { get; set; }
    public string Type { get; set; }
    
    public string Name { get; set; }

    public StockItems(int quantity, string type, string name)
    {
        Id = _newId++;
        Quantity = quantity;
        Type = type;
        Name = name;
    }

    public void DecrementQuantity(int outStock)
    {
        if (Quantity - outStock < 0)
        {
            Console.WriteLine("Not enough stock");
        }
        Quantity -= outStock;
    }
    
    public void IncrementQuantity(int inStock)
    {
        Quantity += inStock;
    }
    
    
    public abstract string Details();

    public override string ToString() => Details();
}
namespace Inventory.Classes;

public class Product : StockItems
{
    public string Brand { get; set; }
    
    public double Price { get; set; }

    public Product(int quantity, string type, string name, string brand, double price) 
        : base(quantity, type, name)
    {
        Brand = brand;
        Price = price;
    }

    public override string Details()
    {
        return $"Id: {Id} \n" +
               $"Type of product: {Type} \n" +
               $"Name of product: {Name} \n" +
               $"Brand: {Brand} \n" +
               $"Price: ${Price} \n" +
               $"Price: ${Quantity} \n";
    }
}
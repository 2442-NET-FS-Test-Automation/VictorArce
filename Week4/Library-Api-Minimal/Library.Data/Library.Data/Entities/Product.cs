using Microsoft.EntityFrameworkCore;
namespace Library.Data.Entities;


//Product will act as a db model - or entity
public class Product
{
    //Do not forget getters and setters
    public int Id { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    //Using a data annotation to enforce a constraint on my column
    //In this case 
    [Precision(10,2)]
    public decimal Price { get; set; }
    
    
    public InventoryItem Inventory { get; set; }
}
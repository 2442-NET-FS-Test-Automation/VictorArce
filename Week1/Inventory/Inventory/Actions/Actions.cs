using Inventory.Classes;

namespace Inventory.Actions;

public class Actions : IActions
{
    private List<StockItems> almacen = new ();
    public bool AddProduct(StockItems product)
    {
        if (almacen.Contains(product))
        {
            Console.WriteLine("Product already exists");
            return false;
        }
        almacen.Add(product);
        Console.WriteLine("Product added");
        return true;
    }

    public bool RestockProduct(int id, int quantity)
    {
        for (int i = 0; i < almacen.Count; i++)
        {
            if (almacen[i].Id == id)
            {
                almacen[i].IncrementQuantity(quantity);
                Console.WriteLine("Product restocked");
                return true;
            }
        }
        Console.WriteLine("Product not found");
        return false;
    }

    public bool SellProduct(int id, int quantity)
    {
        for (int i = 0; i < almacen.Count; i++)
        {
            if (almacen[i].Id == id && almacen[i].Quantity >= quantity)
            {
                almacen[i].DecrementQuantity(quantity);
                return true;
            }
        }
        Console.WriteLine("Product not found or not enough stock");
        return false;
    }

    public void ListProducts()
    {
        foreach (StockItems product in almacen)
        {
            Console.WriteLine(product.Details());
        }
    }
}
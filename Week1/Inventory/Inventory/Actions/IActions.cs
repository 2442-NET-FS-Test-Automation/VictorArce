using Inventory.Classes;

namespace Inventory.Actions;

public interface IActions
{
    public bool AddProduct(StockItems product);
    public bool RestockProduct(int id, int quantity);
    public bool SellProduct(int id, int quantity);
    public void ListProducts();
}
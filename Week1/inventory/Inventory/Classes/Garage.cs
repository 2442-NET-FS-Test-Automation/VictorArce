namespace Inventory.Classes;

//This class is only to fulfill inheritance requirements but its not going to be used by the program
public class Garage : StockItems
{
    public Garage(int quantity, string type, string name) : base(quantity, type, name)
    {
        
    }

    public override string Details()
    {
        throw new NotImplementedException();
    }
}
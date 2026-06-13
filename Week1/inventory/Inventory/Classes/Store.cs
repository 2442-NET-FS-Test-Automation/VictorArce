namespace Inventory.Classes;

public class Store : StockItems
{
    public string Brand { get; set; }
    
    public double Price { get; set; }
    public double DiscountPrice { get; set; }
    
    public bool Discount { get; set; }

    public Store(int quantity, string type, string name, string brand, double price, double discountPrice, bool discount) 
        : base(quantity, type, name)
    {
        Brand = brand;
        Price = price;
        DiscountPrice = discountPrice;
        Discount = discount;
    }

    public override string Details()
    {
        if (Discount)
        {
            return $"{Id} - {Type} - {Name} - {Brand} - {DiscountPrice}";
        }
        return $"{Id} - {Type} - {Name} - {Brand} - {Price}";
    } 
    
    public void AssingDiscount(double discount) => DiscountPrice = (Price * (100 - discount)) / 100;
}
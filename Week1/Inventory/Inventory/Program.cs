
using Inventory.Actions;
using Inventory.Classes;

public class Program
{
    static void Main(string[] args)
    {
        IActions actions = new Actions();
        Product product = new Product(
            10,
            "Electronics",
            "Iphone 13",
            "Apple",
            1000);
        actions.AddProduct(product);
        bool exit = false;
        do
        {
            int opc = 0;
            Console.WriteLine("What do you want to do?");
            Console.WriteLine("1 - Add a product");
            Console.WriteLine("2 - Restock a product");
            Console.WriteLine("3 - Sell a product");
            Console.WriteLine("4 - List products");
            Console.WriteLine("5 - Exit");
            opc = int.Parse(Console.ReadLine());
            switch (opc)
            {
                case 1:
                    Console.WriteLine("Still not implemented because I'm lazy");
                    break;
                case 2:
                    int id = int.Parse(Console.ReadLine());
                    int quantity = int.Parse(Console.ReadLine());
                    actions.RestockProduct(id, quantity);
                    break;
                case 3:
                    actions.SellProduct(1, 1);
                    break;
                case 4:
                    Console.WriteLine("Products:\n");
                    actions.ListProducts();
                    break;
                case 5:
                    exit = true;
                    break;
            }
            Console.ReadKey();
            Console.Clear();
        }while(exit == false);
    }
}


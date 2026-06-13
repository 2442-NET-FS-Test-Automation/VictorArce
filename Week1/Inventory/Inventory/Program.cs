
using Inventory.Actions;
using Inventory.Classes;

public class Program
{
    static void Main()
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
            int id = 0;
            int quantity = 0;
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
                    Console.WriteLine("\n");
                    Console.WriteLine("Enter Id of the product");
                    id = int.Parse(Console.ReadLine());
                    Console.WriteLine("Enter quantity to restock");
                    quantity = int.Parse(Console.ReadLine());
                    Console.WriteLine("\n");
                    actions.RestockProduct(id, quantity);
                    break;
                case 3:
                    Console.WriteLine("\n");
                    Console.WriteLine("Enter Id of the product");
                    id = int.Parse(Console.ReadLine());
                    Console.WriteLine("Enter quantity to sell");
                    quantity = int.Parse(Console.ReadLine());
                    actions.SellProduct(id, quantity);
                    break;
                case 4:
                    Console.WriteLine("Products:");
                    actions.ListProducts();
                    break;
                case 5:
                    exit = true;
                    break;
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }while(exit == false);
    }
}


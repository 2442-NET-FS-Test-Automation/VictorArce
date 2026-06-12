using System.Data.Common;
using Library.Library.Domain;

namespace Library.Library.App;

public class Program
{
    public static void Main(string[] args)
    {
        DataTypesAndOperators();
        ClassesExample();
        OopDemo();
    }
    
    private static void DataTypesAndOperators()
    {
        String tmp = "Texto 1";
        String tmp2 = "Texto 2";
        Console.WriteLine("=== Data  Types and Operators ==");
        
        Console.WriteLine($"{tmp} y no olvidar a {tmp2}");
        
        
    }

    private static void ControlFlow()
    {
        Console.WriteLine("=== ControlFlow ===");
        int copyAvailable = 0;
        bool isMember = true;
        if (copyAvailable > 1 || isMember)
        {
            Console.WriteLine("Many available");
        }
        else if (copyAvailable == 1)
        {
            Console.WriteLine("Last copy");
        }
        else
        {
            Console.WriteLine("Out of stock");
            Console.WriteLine("Check again later");
        }
        
        //Switch
        string genre = "Action";
        
        //Classic switch
        switch (genre)
        {
            case "Action":
                Console.WriteLine("Check section X");    
                break;
            case "Adventure":
                Console.WriteLine("Check section Y");
                break;
            
            default:
                Console.WriteLine("Not found");
                break;
        }
        
        //.net switch

        string section = genre switch
        {
            //rom here its the same
            "Mystery" => "Section A",
            "Romance" => "Section B", 

            _=> "Not found"
        };
        Console.WriteLine(section);
        
    }

    private static void Loops()
    {
        for(int day = 1; day <= 3; day++){
            Console.WriteLine($"Reminder dy {day}: fee so far {CalculateFee(day)}");
        }

        int onShelf = 3;
        while (onShelf > 0)
        {
            Console.WriteLine($"{onShelf} copies available");
            onShelf--;
        }
        Console.WriteLine("No copies left");
        
        
    }

    private static void ArraysWork()
    {
        //Fortunely most of the are already part of the languge
        string[] books = { "Dune", "LOTR", "HP", "Siege of terra" };
        Console.WriteLine(books[2]);
        
        //C# allow for-each hay mi madre el for each
        foreach (string variable in books)
        {
            Console.WriteLine(variable);
        }
    }

    private static void ClassesExample()
    {
        Book Dune = new Book("Dune", "Frank Herbert", 5);
        Console.WriteLine(Dune);
        Book LOTR = new Book("LOTR", "J.R.R. Tolkien", 3);
        Console.WriteLine(LOTR.ToString());
    }

    public static void OopDemo()
    {
        Console.WriteLine("==Oop Demo==");
        //Levegging polymorphism - Books, Reference, Magazine all are LibraryItems
        LibrayItem[] catalog =
        {
            new Book("Dune", "Frank Herbert", 5),
            new Book("LOTR", "J.R.R. Tolkien", 3),
            new ReferenceBook("C# language standards", "Microsoft", "Technology"),
            new Magazine("Sports Ilustrated", "Francisco", 5, "Conde Naste")
        };

        foreach (LibrayItem catalogItem in catalog)
        {
            Console.WriteLine(catalogItem.Describe());
        }

        foreach (LibrayItem catalogItem in catalog)
        {
            if (catalogItem is ILendable lendable)
            {
                Console.WriteLine($"{catalogItem.Title}: checlkout -> {lendable.CheckOut()}");
                Console.WriteLine("This item can be borrowed");
            }
            else
            {
                Console.WriteLine($"{catalogItem.Author} is reference only");
                Console.WriteLine("Only reference");
            }
        }
        
        //Override vs new - behavior
        Magazine wired = new Magazine("Wired", "Luis", 3, "Conde Naste");
        LibrayItem basemag = wired;
        Console.WriteLine("=====Override vs new on the same onject, diferent type=====");
        Console.WriteLine($"Magazine reference -> {wired.ShelfLabel()}");
        Console.WriteLine($"LibrearyItem reference -> {basemag.ShelfLabel()}");
    }
    
    private static decimal CalculateFee(int d) => d * 2;

}
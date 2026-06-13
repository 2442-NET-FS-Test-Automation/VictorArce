using Library.Design_Pattern_Examples;
using Library.Exceptions;
using Library.Library.Domain;
using Library.Repo;
using Serilog;
using Serilog.Configuration;

namespace Library.Library.App;

public class Program
{
    public static void Main(string[] args)
    {
        //Serilog need to be configured before any other things starts
        //Serillog works via a singleton object. its shared globally trought the program
        Log.Logger = 
            new LoggerConfiguration()
                .MinimumLevel
                .Information() // Verbose > Debug > Info > Warnings > Error > Fatal
                .WriteTo.Console() //Sing : where  do my logs go? console, text file, database, etc
                .CreateLogger(); //Create logger using the previous configurtion
        DataTypesAndOperators();
        ClassesExample();
        OopDemo();
        //CollectionsDemo();

        ExceptionsDemo();
        Log.CloseAndFlush();
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
        LibraryItem[] catalog =
        {
            new Book("Dune", "Frank Herbert", 5),
            new Book("LOTR", "J.R.R. Tolkien", 3),
            new ReferenceBook("C# language standards", "Microsoft", "Technology"),
            new Magazine("Sports Ilustrated", "Francisco", 5, "Conde Naste")
        };

        foreach (LibraryItem catalogItem in catalog)
        {
            Console.WriteLine(catalogItem.Describe());
        }

        foreach (LibraryItem catalogItem in catalog)
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
        LibraryItem basemag = wired;
        Console.WriteLine("=====Override vs new on the same onject, diferent type=====");
        Console.WriteLine($"Magazine reference -> {wired.ShelfLabel()}");
        Console.WriteLine($"LibrearyItem reference -> {basemag.ShelfLabel()}");
    }

    private static void CollectionsDemo()
    {
        Console.WriteLine("==== Collections Demo ====");
        //Creating a catalog object
        //Because this is backed by a list,it grows and shrinks automatically
        Catalog catalog = new Catalog();
        
        Book dune = new Book("Dune", "Frank Herbert", 5);

        catalog._items.Add(dune);
        
        catalog._items.Add(new ReferenceBook("C# language standards", "Microsoft", "Technology"));
        
        catalog._items.Add(new Magazine("Nat Geo", "Charlie", 5,  "Conde Naste"));
        
        Console.WriteLine($"Catalog holds {catalog._items.Count} items");
        Console.WriteLine($"the first one is {catalog._items[0].Title}");
        
        //Enum+Struct use
        ItemKind kind = ItemKind.Magazine; //Example of selecting an enum value
        ShelfLocation place = new ShelfLocation(3,12); //Struct 
        Console.WriteLine($"{kind} sits at {place}");

        Book duneCopy = dune;
        //Modifiyng the left one also modifies the right one
        //because they are basically pointers

        ShelfLocation place2 = place;
        //Modifiyng the left one DONT modify the right one because
        //it copied the values

        Shelf<LibraryItem> shelf = new Shelf<LibraryItem>(2);
        Shelf<int> intShelf = new Shelf<int>(100);

        shelf.TryAdd(catalog._items[0]);
        shelf.TryAdd(catalog._items[1]);
        
        //Console.WriteLine($"trying to add a third thing in the catalog: {shelf.TryAdd(catalog._items[2])}");

    }

    public static void ExceptionsDemo()
    {
        Console.WriteLine("==== Exceptions Demo ====");
        
        ILibraryRepository repo = new InMemoryLibraryRepository();
        
        IUnitOfWork libraryWork =  new UnitOfWork(repo);
        
        //Creating a book but using factoory method
        LibraryItem dune = LibraryItemFactory.create(ItemKind.Book, "Dune", "Frank Herber", copies:2);
        repo.Add(dune);
        
        //Magazines needs extra fields but we added default data so we wont be adding it 
        repo.Add(LibraryItemFactory.create(ItemKind.Magazine, "Wired", "Axel", copies:2));
        
        //Pretending to adding channges to database
        libraryWork.Stage("Added 2 items");
        libraryWork.commit();
        
        //We went trought trouble of creating custom exceptions so
        //well try to put it in work wapping some code in a try-catch

        
        //This block will try to catch every kind of exceptions from the most especific to the most generic
        try
        {
            LibraryItem missing = repo.Get(99);
            Console.WriteLine(missing.Describe());
        }
        catch (ItemNotFoundExceptions ex)
        {
            Log.Error("Lookup failed for id {id}: {Message}", ex.Id, ex.Message);
            Console.WriteLine(ex);
            throw;
        }
        catch (LibraryException ex)
        {
            Log.Error("Library error: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Library error: {Message}", ex.Message);
        }
        finally //This will run code wether the code throw an exceptions or not
                //even if theres a return statement in any catch
        {
            Console.WriteLine("Hit out finally block - lookup attempt done");
        }

        Book noCopies = new Book("Count of Montecristo", "Alejandro Dumas", 0);

        try
        {
            Borrow(noCopies);
        }
        catch (ItemNotAvailableException ex)
        {
            Log.Warning("Borrow refused; {Message}", ex.Message);
            throw;
        }
    }

    public static void Borrow(Book book)
    {
        if (!book.CheckOut())
        {
            throw new ItemNotAvailableException(book.Title);
        }
    }
    
    private static decimal CalculateFee(int d) => d * 2;

}
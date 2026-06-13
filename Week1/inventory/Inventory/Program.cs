using Inventory.Classes;

if (true)
{
    test();
}

static void test()
{
    Console.WriteLine("Welcome to the Inventory Program!");
    StockItems[] storage =
    {
        new Store(10, "Car", "Ford", "Ford", 1000, 900, true),
        new Store(10, "Car", "Chevy", "Chevy", 1200, 1100, false),
        new Store(10, "Car", "BMW", "BMW", 1500, 1400, false),
        new Store(10, "Car", "Tesla", "Tesla", 2000, 1900, false),
        new Store(10, "Car", "Audi", "Audi", 2500, 2400, false),
        new Store(10, "Car", "Subaru", "Subaru", 3000, 2900, false),
        new Store(10, "Car", "Kia", "Kia", 3500, 3400, false),
    };

    foreach (var item in storage)
    {
        Console.WriteLine(item.Details());
    }
}
using Microsoft.EntityFrameworkCore;
using Library.Data;
//This is my API program.cs
//No main. we can think of it as 2 sections
//Registering things with the builder
//And then configuring things on the app
//And at the very bottom that app object that represents our entire API calls its run method

//Builder are
var builder = WebApplication.CreateBuilder(args);

//The first thing that we need to give our builder a connection string to our database
string connectionString = 
    builder.Configuration.GetConnectionString("Server=localhost,1433;" +
                                              "Database=LibraryMinimalDB;" +
                                              "User Id=sa;" +
                                              "Password=Gatito_1433!" +
                                              "TrustServerCertificate=true");

var conn_string = connectionString;

//Tell the builder to use our LibraryDBConext with the connection string above
builder.Services.AddDbContext<LibraryDBContext>(options => options.UseSqlServer(conn_string));

//Swagger stuff
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//App area
var app = builder.Build();

//Swagger stuff added to app
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World!");

app.MapGet("/inventory", async (LibraryDBContext db) =>
{
    //We should probably await this call since it is async and we are in a web api
    return await db.Inventory.ToListAsync();
});

//Lets use LINQ - language integrated query
//LINQ is a library that just lets us query our data in a more readable way
//The logic actually flows from sql DQL - you can use method OR sql query syntax
//You caneven save the queries themselves as C# objects if you want to
app.MapGet("/Inventory/By-Value", async (LibraryDBContext db) =>
{
    return await db.Inventory.Include(i => i.Product)
        .GroupBy(i => i.QuantityOnHand >= 5 ? "Well-stocked" : "Low Stock")
        .Select(g => new (tier = g.Key, count = g.Count(), units = g.Sum(i =>  i.CurentStock))
        .ToList()
        );
});

app.MapGet("/peek/tracking", (LibraryDBContext db) =>
{
    var unchanged = db.Products.First(); 
    var modified = db.Products.Skip(1).First();
    modfied.price += 1.00m; //state => Modified

    db.Products.Add(new Product { Sku = "SK - TMP", Name = "New Product", Price = 9.99m }); //state => Added

    //This bit of code is the non-nonproduction demo
    //We are accesing the LibraryDBContext's ChangeTracker to pull info
    //At most youd debug with this.
    var states = db.ChangeTracker.Entries()
        .Select(e => new { entity = e.Entity.GetType().Name, state = e.State.ToString() })
        .ToList();

    //Clearing the change tracker manually
    db.ChangeTracker.Clear();
    return states;
});

//Lets manually go out of our way o create a conflict. DONT DO THIS ON A REAL APP
app.MapGet("/peek/conflict", (IServiceScopeFactory scope) =>
{
    //Manually asking for scopes, normally each endpoint method call gets its own scope
    //tracked by ASP.NET under the hood during runtime. We can, for various reasons good or bad
    //do this manually
    using var scope1 = scope.CreateScope();
    using var scope2 = scope.CreateScope();

    var firstdb = scope1.ServiceProvider.GetRequiredService<LibraryDBContext>();
    var seconddb = scope2.ServiceProvider.GetRequiredService<LibraryDBContext>();

    var firstinventory = firstdb.Inventory.First(i => i.Id == 1);
    var secondinventory = seconddb.Inventory.First(i => i.Id == 1);

    firstinventory.CurentStock --;
    firstdb.SaveChanges(); //SAveChanges is what persist any created, deleted or modified objects
    //That row in the DB now has a RowVersion of 2

    //Calling savechanges aabove modifies the RowVersion value
    //this object, that should represent the exact 

    secondinventory.CurentStock --;

    try
    {
        seconddb.SaveChanges();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        //In this case we want EF to retry the UPDATE()
        //Asking for the actual ChangeTracker entry that caused the exception
        //This is EF Core specific
        var entry = ex.Entries.Single();

        //For the entry that threw the exception grab its current values from DB
        //Not the object just the values
        var current = entry.GetDatabaseValues(); //EF will go to the database and get the current values for that row

        //Every entry in the change tracker tracks 2 sets of values, t
        // he original values = THE VALUES OF THE OBJECT WHEN IT WAS LOADED FROM DB
        // and the current values = THE NEW MODIFIED VALUES WE CHANGED ON THE OBJECT IN OUR APP
        //Here we manually set the OriginalValues to the values from DB we just grabbed
        entry.OriginalValues.SetValues(current!); //EF will update the original values of the object to match the current values in the database

        //Using the entry to grab the actual the actual item - going somewhat backkwards
        ((InventoryItem)entry.Entity).CurentStock =
            current!.GetValue<int>(nameof(InventoryItem.CurentStock)) - 1; //We are now modifying the current values of the object to be the current value in the database minus 1
    
        seconddb.SaveChanges(); //EF will now update the row in the database with the new values we set on the object
    }
    //I can send back specific codes via methods like OK() or NotFound() or BadRequest() or Conflict() etc
    //with messages inside
    return Results.Ok("Conflict resolved, the row was updated with the new values");


});

app.Run();
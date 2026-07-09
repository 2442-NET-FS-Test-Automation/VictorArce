using Microsoft.EntityFrameworkCore;
using Week3Project.Data;
using Serilog;
using Week3Project.Api.Fulfillment;
using Week3Project.Api.Seeder;


var builder = WebApplication.CreateBuilder(args);

//String for connection to the server, IT SHOUlD BE ON A .env FILE AND NOT HERE!
//It's already moved to appsetings.json, but I'll leave the note here
//var for the DB connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//Connection to SQL server
//Add the DB context
//builder.Services.AddDbContext<StoreDbContext>(options =>
//    options.UseSqlServer(connectionString));
//Commented just to se if the whole thing works with ContextFactory
builder.Services.AddDbContextFactory<StoreDbContext>(options => 
    options.UseSqlServer(connectionString));

//Serilog initialization
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

//Here goes the swagger stuff
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "\n\n\"From the moment I understood the weakness of my flesh, it disgusted me. I craved the strength and certainty of steel. I aspired to the purity of the blessed machine. Your kind cling to your flesh as if it will not decay and fail you. One day the crude biomass you call a temple will wither and you will beg my kind to save you. But I am already saved. For the Machine is Immortal\"\n");

//CRUD stuff
//List all cards in the DB
app.MapGet("/Cards", async (IDbContextFactory<StoreDbContext> factory, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting all cards");
    using var db = await factory.CreateDbContextAsync();
    return await db.Cards.ToListAsync();
});

app.MapPost("/Cards/Burst", (int n, bool expedited, ISeeder seeder, IServiceScopeFactory scopeFactory
,IHostApplicationLifetime appLifetime) =>
{
    Log.Information("Seeding the database with {n} orders", n);
    var ids = seeder.Seed(n, expedited);
    var appStopping = appLifetime.ApplicationStopping;

    Log.Information("Seeding completed");
    Log.Information("Starting BurstPlanner");
    _ = Task.Run(async () =>
    {
        try
        {
            Log.Information("Getting scope");
            using var scope = scopeFactory.CreateScope();
            Log.Information("Getting service");
            var service = scope.ServiceProvider.GetRequiredService<BurstPlanner>();
            //await service.FulfillBurstAsync
        }
        catch (Exception e)
        {
            Log.Error(e, "Burst Failed");
        }
    }, appStopping
        );
});

//List all cards in the DB with their quantity in stock
app.MapGet("/Cards/withQuantity", async (StoreDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting all cards with their quantity in stock");
    return await db.Cards
        .Select(c => new 
        {
            c.Sku,
            c.Name,
            c.Inventory.QuantityOnHand
            // Assuming a separate Inventory table matches on CardId
        })
        .ToListAsync();
});

app.MapGet("/Cards/withStock", async (StoreDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting all cards with stock greater than 0");
    return await db.Cards
        .Select(c => new 
        {
            c.Sku,
            c.Name,
            c.Inventory.QuantityOnHand
            // Assuming a separate Inventory table matches on CardId
        }).Where(inventory => inventory.QuantityOnHand > 0)
        .ToListAsync();
});

app.MapGet("/Cards/whitoutStock", async (StoreDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting all cards with stock equal to 0");
    return await db.Cards
        .Select(c => new
        {
            c.Sku,
            c.Name,
            c.Inventory.QuantityOnHand
        }).Where(inventory => inventory.QuantityOnHand == 0)
        .ToListAsync();
});

app.MapGet("/Customers", async (StoreDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Getting all customers");
    return await db.Customer.ToListAsync();
});

app.MapGet("/Reset", async (StoreDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Trying to reset the inventory to initial value");
    foreach (var card in db.Cards)
    {
        switch (card.Id)
        {
            case 1:
                card.Inventory.QuantityOnHand = 20;
                break;
            case 2:
                card.Inventory.QuantityOnHand = 0;
                break;
            case 3:
                card.Inventory.QuantityOnHand = 3;
                break;
            case 4:
                card.Inventory.QuantityOnHand = 8;
                break;
            default:
                break;
        }
    }
    await db.SaveChangesAsync();
    logger.LogInformation("Inventory reset to initial value");
    return Results.Ok();
});

//Create - Post
// app.MapPost("/cards", async (Card newCard, StoreDbContext db) =>
// {
//     db.Cards.Add(newCard);
//     await db.SaveChangesAsync();
//     return Results.Created($"/cards/{newCard.Id}", newCard);
// });

//Read - Get


//Update - Put

//Delete - Delete


//app.Run() always has to be at the end of the file either on a minimal API or a Controller Api
app.Run();

//Praise Ommnisiah
Log.CloseAndFlush();

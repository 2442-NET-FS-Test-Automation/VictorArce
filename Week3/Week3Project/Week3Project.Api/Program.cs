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
//Ok, so the program shouldn't be able to even compile due to StoreDbContext being commented, but 
//somehow it compiles and even make the queries as usual and even weirder than that, the program
//cannot be compiled if I uncomment the line above so, ill just leave it here in case somone wants to test it
builder.Services.AddDbContextFactory<StoreDbContext>(options => 
    options.UseSqlServer(connectionString));

//Injecting Seeder dependency
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<IBurstPlanner, BurstPlanner>();
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();

//Serilog initialization
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

//Here goes the swagger stuff
app.UseSwagger();
app.UseSwaggerUI();

//Hay mi madre que el espiritu de la maquina le conceda a este codigo la capacidad de no fallar
app.MapGet("/", () => "\n\n\"From the moment I understood the weakness of my flesh, it disgusted me. I craved the strength and certainty of steel. I aspired to the purity of the blessed machine. Your kind cling to your flesh as if it will not decay and fail you. One day the crude biomass you call a temple will wither and you will beg my kind to save you. But I am already saved. For the Machine is Immortal\"\n");

//CRUD stuff
//List all cards in the DB
app.MapGet("/Cards", async (StoreDbContext db) =>
{
    return await db.Cards.ToListAsync();
});

//List all cards in the DB with their quantity in stock
app.MapGet("/Cards/withQuantity", async (StoreDbContext db) =>
{
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

app.MapGet("/Cards/withStock", async (StoreDbContext db) =>
{
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

app.MapGet("/Cards/whitoutStock", async (StoreDbContext db) =>
{
    return await db.Cards
        .Select(c => new
        {
            c.Sku,
            c.Name,
            c.Inventory.QuantityOnHand
        }).Where(inventory => inventory.QuantityOnHand == 0)
        .ToListAsync();
});

app.MapGet("/Customers", async (StoreDbContext db) =>
{
    return await db.Customer.ToListAsync();
});

app.MapPost("/Seed", async () =>
{
    
});

app.MapPost("/Burst", async (int i, bool expedited, IServiceScopeFactory scopeFactory, ISeeder seeder,
    IHostApplicationLifetime appLifetime) =>
{
    //Logs for the records
    Log.Information("Starting burst. Seeding {i} orders to DataBase.", i);
    
    //Execute that aberration we call seeder
    IReadOnlyList<int> ids = seeder.Seed(i, expedited);
    
    //Ask for the cancellation token
    var cts = appLifetime.ApplicationStopping;
    
    Log.Information("Seeded complete. {Count} orders to the planifier.", ids.Count);
    
    _ = Task.Run(async () =>
    {
        try
        {
            // Creamos un entorno seguro aislado para este hilo de fondo
            using var scope = scopeFactory.CreateScope();
            var planner = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();
            
            // Pasamos la lista de IDs y el token de apagado al motor
            await planner.ProcessBurstAsync(ids, cts);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("El procesamiento de la ráfaga fue interrumpido limpiamente debido al apagado del servidor.");
        }
        catch (Exception e)
        {
            Log.Error(e, "Ocurrió un error crítico inesperado durante el procesamiento de la ráfaga.");
        }
    }, cts);
});


//app.Run() always has to be at the end of the file either on a minimal API or a Controller Api
app.Run();

//Praise Ommnisiah
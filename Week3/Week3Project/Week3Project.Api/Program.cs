using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Week3Project.Data;
using Serilog;
using Week3Project.Api.Fulfillment;
using Week3Project.Api.Seeder;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;
using Week3Project.Api.Exceptions;


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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
List<Card> cardsRefill = new List<Card>
{
    new Card { Sku = "YGO-GAOV-EN032-UR", Name = "Neo Galaxy-Eyes Photon Dragon", Price = 10m},
    new Card { Sku = "YGO-GAOV-EN006-UR", Name = "Cardcar D", Price = 4m},
    new Card { Sku = "YGO-GAOV-EN047-SP", Name = "Hieratic Dragon King of Atum", Price = 4m},
    new Card { Sku = "YGO-GAOV-EN011-SP", Name = "Lightray Sorcerer" , Price = 1m},
    new Card { Sku = "YGO-GAOV-ENSP2-UR", Name = "Hieratic Seal of the Dragon King", Price = 9m}
};

List<CardInventory> CardInventories = new List<CardInventory>
{
    new CardInventory { CardId = 5, QuantityOnHand = 5 },
    new CardInventory { CardId = 6, QuantityOnHand = 45 }, 
    new CardInventory { CardId = 7, QuantityOnHand = 150 },
    new CardInventory { CardId = 8, QuantityOnHand = 300 },
    new CardInventory { CardId = 9, QuantityOnHand = 500 } 
};

//Serilog initialization
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

//The global exception handler
app.UseExceptionHandler();

//Here goes the swagger stuff
app.UseSwagger();
app.UseSwaggerUI();

//Hay mi madre que el espiritu de la maquina le conceda a este codigo la capacidad de no fallar
app.MapGet("/", () => "\n\n\"From the moment I understood the weakness of my flesh, it disgusted me. I craved the strength and certainty of steel. I aspired to the purity of the blessed machine. Your kind cling to your flesh as if it will not decay and fail you. One day the crude biomass you call a temple will wither and you will beg my kind to save you. But I am already saved. For the Machine is Immortal\"\n");

//Map stuff
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
            c.Inventory!.QuantityOnHand
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
            c.Inventory!.QuantityOnHand
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
            c.Inventory!.QuantityOnHand
        }).Where(inventory => inventory.QuantityOnHand == 0)
        .ToListAsync();
});

app.MapGet("/Customers", async (StoreDbContext db) =>
{
    return await db.Customer.ToListAsync();
});

app.MapGet("/reports/top-products", async (StoreDbContext db, CancellationToken ctk) =>
{
    Log.Information("Generating Top Products report...");

    // Grouping by CardId and Name, calculating the sum of quantities, and sorting descending
    var topProducts = await db.OrderLines
        // Optional: Uncomment the next line if you ONLY want to count successfully fulfilled orders
        // .Where(ol => ol.Order.Status == OrderStatus.Fulfilled)
        .GroupBy(ol => new { ol.CardId, ol.Card.Name })
        .Select(g => new
        {
            CardId = g.Key.CardId,
            CardName = g.Key.Name,
            TotalUnitsAllocated = g.Sum(ol => ol.Quantity),
            TimesOrdered = g.Count() // How many separate orders included this item
        })
        .OrderByDescending(x => x.TotalUnitsAllocated)
        .Take(10) // Standard practice to limit reports to Top 10/50 to save bandwidth
        .ToListAsync(ctk);

    if (!topProducts.Any())
    {
        return Results.Ok(new { Message = "No product data available yet. Run a burst first!" });
    }

    return Results.Ok(new
    {
        Report = "Top Products",
        Timestamp = DateTime.UtcNow,
        Count = topProducts.Count,
        Data = topProducts
    });
});

app.MapPost("/Seed", async (StoreDbContext db) =>
{
    Log.Information("Starting safe database seed (preserving existing records)...");

    try
    {
        // 1. Seed Cards (checking by SKU)
        var existingCardSkus = await db.Cards.Select(c => c.Sku).ToListAsync();
        int addedCards = 0;

        Log.Information("Seeding missing cards...");
        foreach (Card tmp in cardsRefill)
        {
            if (!existingCardSkus.Contains(tmp.Sku))
            {
                db.Cards.Add(tmp);
                addedCards++;
            }
        }
        
        // Save cards so the DB assigns them their REAL auto-incremented Ids!
        if (addedCards > 0) 
        {
            await db.SaveChangesAsync();
        }

        // 2. Map your specific quantities to the Card SKUs 
        // (Assuming they match your cardsRefill list order 1-to-1)
        var inventoryDemands = new Dictionary<string, int>
        {
            { "YGO-GAOV-EN032-UR", 5 },
            { "YGO-GAOV-EN006-UR", 45 },
            { "YGO-GAOV-EN047-SP", 150 },
            { "YGO-GAOV-EN011-SP", 300 },
            { "YGO-GAOV-ENSP2-UR", 500 }
        };

        var existingInventoryCardIds = await db.Inventories.Select(i => i.CardId).ToListAsync(); 
        var allCards = await db.Cards.ToListAsync(); // Get the cards with their real DB Ids!
        int addedInventories = 0;

        Log.Information("Seeding missing inventory dynamically...");
        foreach (var card in allCards)
        {
            // If this card is one of the ones we want to track AND it has no inventory row yet
            if (inventoryDemands.TryGetValue(card.Sku, out int targetQty))
            {
                if (!existingInventoryCardIds.Contains(card.Id))
                {
                    db.Inventories.Add(new CardInventory 
                    { 
                        CardId = card.Id,           // Uses the REAL database ID!
                        QuantityOnHand = targetQty 
                    });
                    addedInventories++;
                }
            }
        }
        
        if (addedInventories > 0)
        {
            Log.Information("Saving {Invs} new inventory rows...", addedInventories);
            await db.SaveChangesAsync();
        }
        
        return Results.Ok(new { 
            Message = "Seed executed safely. FK constraints satisfied.",
            CardsAdded = addedCards,
            InventoriesAdded = addedInventories
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to execute database seed.");
        return Results.StatusCode(500);
    }
});

//Easy as it reads, set all the stock to 0
app.MapPost("/Clear", async (IDbContextFactory<StoreDbContext> dbContextFactory) =>
{
    Log.Information("Purging all card stock to zero...");

    try
    {
        using var db = await dbContextFactory.CreateDbContextAsync();
        
        // Load the inventories into memory
        var inventories = await db.Inventories.ToListAsync();
        
        foreach (var inventory in inventories)
        {
            inventory.QuantityOnHand = 0;
        }

        // CRITICAL: Push changes to SQL Server!
        await db.SaveChangesAsync();

        Log.Information("All vault quantities successfully zeroed out.");
        return Results.Ok(new { Message = "All card stock set to 0." });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to clear inventory quantities.");
        return Results.StatusCode(500);
    }
});

app.MapPost("/Restock", async (StoreDbContext db) =>
{
    Log.Information("Initiating global restock: setting all inventory rows to 1000 units...");
    try
    {
        // Executes "UPDATE Inventories SET QuantityOnHand = 10" directly in the database
        int affectedRows = await db.Inventories.ExecuteUpdateAsync(setters => 
            setters.SetProperty(i => i.QuantityOnHand, 1000));

        Log.Information("Global restock complete. {Count} card inventory rows set to 1000.", affectedRows);
        return Results.Ok(new { Message = "All card quantities successfully set to 1000.", RowsUpdated = affectedRows });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to execute global restock operation.");
        return Results.StatusCode(500);
    }
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
            Log.Information("Starting burst");
            // Creamos un entorno seguro aislado para este hilo de fondo
            using var scope = scopeFactory.CreateScope();
            var planner = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();
            
            // Pasamos la lista de IDs y el token de apagado al motor
            await planner.ProcessBurstAsync(ids, cts);
            Log.Information("Burst made it");
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

    Log.Information("Burst completed");
});

app.MapPost("/Benchmark", async (int n, ISeeder seeder, IFulfillmentService fulfillmentService, 
    StoreDbContext db, HttpContext httpContext) =>
{
    // Use the HTTP request's built-in cancellation token.
    // If the client cancels the request or closes the tab, processing aborts instantly!
    var ctk = httpContext.RequestAborted;

    Log.Information("=== STARTING TARGET M4 BENCHMARK ===");
    Log.Information("Generating a mixed batch of {Count} benchmark test orders...", n);

    // 1. Generate a test pool of 'n' orders using your seeder
    // We generate a mixed batch to ensure priority lanes are engaged during the parallel run
    IReadOnlyList<int> sampleOrderIds = seeder.Seed(n, expedited: true);

    if (!sampleOrderIds.Any())
    {
        Log.Warning("Seeder returned 0 orders. Aborting benchmark calculation.");
        return Results.BadRequest(new { Error = "Could not generate sample test orders." });
    }

    // --- PHASE 1: THE SEQUENTIAL RUN ---
    Log.Information("Phase 1: Initializing baseline stock for Sequential Run...");
    // Reset all card inventory rows to a stable baseline (e.g., 500 units) so they don't immediately backorder
    await db.Inventories.ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantityOnHand, 500), ctk);
    
    // Ensure the generated orders are in a clean Pending state
    await db.Orders.Where(o => sampleOrderIds.Contains(o.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Pending), ctk);

    Log.Information("Executing Sequential processing pipeline...");
    var sequentialTimer = Stopwatch.StartNew();
    
    // Run the fulfillment service sequentially (useParallel: false)
    await fulfillmentService.ProcessBurstAsync(sampleOrderIds, ctk, useParallel: false);
    
    sequentialTimer.Stop();
    long sequentialMs = sequentialTimer.ElapsedMilliseconds;
    Log.Information("Sequential Run completed in {Elapsed}ms.", sequentialMs);


    // --- PHASE 2: THE PARALLEL RUN ---
    Log.Information("Phase 2: Resetting stock variables for Parallel Run...");
    // CRITICAL REQUIREMENT: Reset stock to the exact same starting values to keep the test fair
    await db.Inventories.ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantityOnHand, 500), ctk);
    
    // Reset the exact same orders back to Pending so the parallel engine can process them from scratch
    await db.Orders.Where(o => sampleOrderIds.Contains(o.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Pending), ctk);

    Log.Information("Executing Parallel processing pipeline...");
    var parallelTimer = Stopwatch.StartNew();
    
    // Run the fulfillment service concurrently (useParallel: true)
    await fulfillmentService.ProcessBurstAsync(sampleOrderIds, ctk, useParallel: true);
    
    parallelTimer.Stop();
    long parallelMs = parallelTimer.ElapsedMilliseconds;
    Log.Information("Parallel Run completed in {Elapsed}ms.", parallelMs);


    // --- PHASE 3: ANALYSIS & REPORTING ---
    // Calculate the efficiency multiplier (Speedup Factor)
    double speedupFactor = (double)sequentialMs / (parallelMs == 0 ? 1 : parallelMs);

    Log.Information("=== BENCHMARK COMPLETE ===");
    Log.Information("Resulting Speedup Factor: {Speedup:F2}x", speedupFactor);

    return Results.Ok(new
    {
        Message = "Benchmark completed successfully.",
        TotalOrdersProcessed = n,
        InitialStockPerCard = 500,
        SequentialTimeMs = sequentialMs,
        ParallelTimeMs = parallelMs,
        SpeedupFactor = Math.Round(speedupFactor, 2),
        PerformanceNotes = speedupFactor > 1.0 
            ? "The parallel machine spirit outpaced the sequential flesh." 
            : "Parallel overhead exceeded task payload. Try scaling 'n' higher to let the threads open up!"
    });
});

//app.Run() always has to be at the end of the file either on a minimal API or a Controller Api
app.Run();
Log.Information("The machine spirit let us compile this and test it");
Log.CloseAndFlush();

//Praise Ommnisiah
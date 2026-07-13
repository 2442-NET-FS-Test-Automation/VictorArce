using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Week3Project.Data;
using Serilog;
using Week3Project.Api.Fulfillment;
using Week3Project.Api.Seeder;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;
using Week3Project.Api.Exceptions;
using Scalar.AspNetCore;


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

// ==========================================
// 1. DEPENDENCY INJECTION & SERVICES REGISTER
// ==========================================

// Register application services with Scoped lifetime (a new instance is created per HTTP request)
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddScoped<IBurstPlanner, BurstPlanner>();
builder.Services.AddScoped<IFulfillmentService, FulfillmentService>();

// Register global exception handling components to intercept and format unhandled runtime errors
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // Generates RFC 7807 compliant error responses

// ==========================================
// 2. DATA STRUCTURES & STATIC SEED POOLS
// ==========================================

// Static reference list of Yu-Gi-Oh cards used to re-populate the database during seeding
List<Card> cardsRefill = new List<Card>
{
    new Card { Sku = "YGO-GAOV-EN032-UR", Name = "Neo Galaxy-Eyes Photon Dragon", Price = 10m},
    new Card { Sku = "YGO-GAOV-EN006-UR", Name = "Cardcar D", Price = 4m},
    new Card { Sku = "YGO-GAOV-EN047-SP", Name = "Hieratic Dragon King of Atum", Price = 4m},
    new Card { Sku = "YGO-GAOV-EN011-SP", Name = "Lightray Sorcerer" , Price = 1m},
    new Card { Sku = "YGO-GAOV-ENSP2-UR", Name = "Hieratic Seal of the Dragon King", Price = 9m}
};

// Static initial stock quantities assigned to inventory slots
List<CardInventory> CardInventories = new List<CardInventory>
{
    new CardInventory { CardId = 5, QuantityOnHand = 5 },
    new CardInventory { CardId = 6, QuantityOnHand = 45 }, 
    new CardInventory { CardId = 7, QuantityOnHand = 150 },
    new CardInventory { CardId = 8, QuantityOnHand = 300 },
    new CardInventory { CardId = 9, QuantityOnHand = 500 } 
};

// ==========================================
// 3. LOGGING & API EXPLORER SETUP
// ==========================================

// Initialize Serilog to capture runtime output to both the Console and daily rolling log files
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fulfillment-log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Configure OpenAPI (Swagger/Scalar) tools for API specification generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ==========================================
// 4. MIDDLEWARE PIPELINE CONFIGURATION
// ==========================================

// Activates the GlobalExceptionHandler registered in the service collection
app.UseExceptionHandler();

// Enable API documentation interactive UIs only when running in a local Development environment
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ==========================================
// 5. API ROUTE ENDPOINTS (MINIMAL APIs)
// ==========================================

// Root endpoint serving as an application health check / easter egg
app.MapGet("/", () => "\n\n\"From the moment I understood the weakness of my flesh, " +
                      "it disgusted me. I craved the strength and certainty of steel. " +
                      "I aspired to the purity of the blessed machine. " +
                      "Your kind cling to your flesh as if it will not decay and fail you. One day the crude biomass you call a temple will wither and you will beg my kind to save you. " +
                      "But I am already saved. " +
                      "For the Machine is Immortal\"\n");

// GET: Fetches a complete list of all cards tracked in the database
app.MapGet("/Cards", async (StoreDbContext db) =>
{
    return await db.Cards.ToListAsync();
});

// GET: Projection query that pulls card details alongside their total inventory counts
app.MapGet("/Cards/withQuantity", async (StoreDbContext db) =>
{
    return await db.Cards
        .Select(c => new 
        {
            c.Sku,
            c.Name,
            c.Inventory!.QuantityOnHand
        })
        .ToListAsync();
});

// GET: Filters and returns only cards that currently have 1 or more units available in stock
app.MapGet("/Cards/withStock", async (StoreDbContext db) =>
{
    return await db.Cards
        .Select(c => new 
        {
            c.Sku,
            c.Name,
            c.Inventory!.QuantityOnHand
        }).Where(inventory => inventory.QuantityOnHand > 0)
        .ToListAsync();
});

// GET: Filters and returns cards that are completely out of stock (Quantity == 0)
app.MapGet("/Cards/withoutStock", async (StoreDbContext db) =>
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

// GET: Fetches a complete list of all registered store customers
app.MapGet("/Customers", async (StoreDbContext db) =>
{
    return await db.Customer.ToListAsync();
});

// GET: Analytics report showing the top 10 products sorted by total volume of units ordered
app.MapGet("/reports/top-products", async (StoreDbContext db, CancellationToken ctk) =>
{
    Log.Information("Generating Top Products report...");

    var topProducts = await db.OrderLines
        .GroupBy(ol => new { ol.CardId, ol.Card.Name })
        .Select(g => new
        {
            CardId = g.Key.CardId,
            CardName = g.Key.Name,
            TotalUnitsAllocated = g.Sum(ol => ol.Quantity),
            TimesOrdered = g.Count() 
        })
        .OrderByDescending(x => x.TotalUnitsAllocated)
        .Take(10) // Restrict to top 10 items to preserve network bandwidth
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

// POST: Idempotent database seeder that safely populates missing cards and target stock counts
app.MapPost("/Seed", async (StoreDbContext db) =>
{
    Log.Information("Starting database seed ...");

    try
    {
        // PHASE 1: Verify missing records by SKU to prevent duplicate primary key insertion errors
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
        
        // Persist records immediately so relational database engines assign real identity keys
        if (addedCards > 0) 
        {
            await db.SaveChangesAsync();
        }

        // PHASE 2: Map SKU strings to strict relational foreign keys (Inventory requirements)
        var inventoryDemands = new Dictionary<string, int>
        {
            { "YGO-GAOV-EN032-UR", 5 },
            { "YGO-GAOV-EN006-UR", 45 },
            { "YGO-GAOV-EN047-SP", 150 },
            { "YGO-GAOV-EN011-SP", 300 },
            { "YGO-GAOV-ENSP2-UR", 500 }
        };

        var existingInventoryCardIds = await db.Inventories.Select(i => i.CardId).ToListAsync(); 
        var allCards = await db.Cards.ToListAsync(); 
        int addedInventories = 0;

        Log.Information("Seeding missing inventory dynamically...");
        foreach (var card in allCards)
        {
            if (inventoryDemands.TryGetValue(card.Sku, out int targetQty))
            {
                if (!existingInventoryCardIds.Contains(card.Id))
                {
                    db.Inventories.Add(new CardInventory 
                    { 
                        CardId = card.Id, // Link using the dynamic ID provided by DB engine
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

// POST: Flushes all inventory counts back to zero
app.MapPost("/Clear", async (IDbContextFactory<StoreDbContext> dbContextFactory) =>
{
    Log.Information("Purging all card stock to zero...");

    try
    {
        // Creates an isolated database context thread manually to manage state safe from concurrency conflicts
        using var db = await dbContextFactory.CreateDbContextAsync();
        
        var inventories = await db.Inventories.ToListAsync();
        foreach (var inventory in inventories)
        {
            inventory.QuantityOnHand = 0;
        }

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

// POST: High-performance direct SQL execution to restock all items back to 1000 items instantly
app.MapPost("/Restock", async (StoreDbContext db) =>
{
    Log.Information("Initiating global restock: setting all inventory rows to 1000 units...");
    try
    {
        // Executes an in-database UPDATE statement bypassing standard EF entity tracking overhead
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

// POST: Generates massive volume orders and routes them off to background processing threads asynchronously
app.MapPost("/Burst", async (int i, bool expedited, IServiceScopeFactory scopeFactory, ISeeder seeder,
        IHostApplicationLifetime appLifetime) =>
{
    Log.Information("Starting burst. Seeding {i} orders to DataBase.", i);
    
    // Step 1: Populate incoming workload orders synchronously during the request context
    IReadOnlyList<int> ids = seeder.Seed(i, expedited);
    
    // Tie process lifecycle tokens to background threads to handle safe shutdowns gracefully
    var cts = appLifetime.ApplicationStopping;
    
    Log.Information("Seeding complete. {Count} orders sent to the background planner.", ids.Count);
    
    // Step 2: Offload heavy processing execution loop onto separate background workers
    _ = Task.Run(async () =>
    {
        try
        {
            // Create a completely separate DI Scope for the thread. 
            // This prevents memory corruption and resource disposal failures with the DbContext.
            using var scope = scopeFactory.CreateScope();
            var fulfillmentService = scope.ServiceProvider.GetRequiredService<IFulfillmentService>();
            
            await fulfillmentService.ProcessBurstAsync(ids, cts);
            Log.Information("Background burst execution completed successfully.");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Burst processing was cleanly interrupted due to server shutdown.");
        }
        catch (Exception e)
        {
            Log.Error(e, "A critical unexpected error occurred during background burst processing.");
        }
    }, cts);

    // Return 202 Accepted status instantly so api client doesn't time out waiting for fulfillment processing
    return Results.Accepted(value: new { Message = "Burst processing started in background.", OrderCount = ids.Count });
});

// POST: Diagnostic utility comparing Sequential processing throughput vs Parallel processing pipelines
app.MapPost("/Benchmark", async (int n, ISeeder seeder, IFulfillmentService fulfillmentService, 
    StoreDbContext db, HttpContext httpContext) =>
{
    // Capture user cancellation tokens (If client closes browser tab/aborts request, performance measurements stop)
    var ctk = httpContext.RequestAborted;
    
    Log.Information("Generating a mixed batch of {Count} benchmark test orders...", n);

    IReadOnlyList<int> sampleOrderIds = seeder.Seed(n, expedited: true);

    if (!sampleOrderIds.Any())
    {
        Log.Warning("Seeder returned 0 orders. Aborting benchmark calculation.");
        return Results.BadRequest(new { Error = "Could not generate sample test orders." });
    }

    // --- PHASE 1: THE SEQUENTIAL RUN ---
    Log.Information("Phase 1: Initializing baseline stock for Sequential Run...");
    await db.Inventories.ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantityOnHand, 500), ctk);
    await db.Orders.Where(o => sampleOrderIds.Contains(o.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Pending), ctk);

    Log.Information("Executing Sequential processing pipeline...");
    var sequentialTimer = Stopwatch.StartNew();
    
    // Run order processing using a single processing core channel loop
    await fulfillmentService.ProcessBurstAsync(sampleOrderIds, ctk, useParallel: false);
    
    sequentialTimer.Stop();
    long sequentialMs = sequentialTimer.ElapsedMilliseconds;
    Log.Information("Sequential Run completed in {Elapsed}ms.", sequentialMs);

    // --- PHASE 2: THE PARALLEL RUN ---
    Log.Information("Phase 2: Resetting stock variables for Parallel Run...");
    // CRITICAL: Reset database back to baseline so that thread contention constraints match identical baselines
    await db.Inventories.ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantityOnHand, 500), ctk);
    await db.Orders.Where(o => sampleOrderIds.Contains(o.Id))
        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Pending), ctk);

    Log.Information("Executing Parallel processing pipeline...");
    var parallelTimer = Stopwatch.StartNew();
    
    // Run order processing scaling workloads across multi-threaded asynchronous workers concurrently
    await fulfillmentService.ProcessBurstAsync(sampleOrderIds, ctk, useParallel: true);
    
    parallelTimer.Stop();
    long parallelMs = parallelTimer.ElapsedMilliseconds;
    Log.Information("Parallel Run completed in {Elapsed}ms.", parallelMs);

    // --- PHASE 3: ANALYSIS & REPORTING ---
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

// ==========================================
// 6. LIFE CYCLE & ROOT PROCESS EXECUTION
// ==========================================
try
{
    Log.Information("Starting the machine spirit application context...");
    
    // Blocking initialization process loop that starts web socket pipelines listening for traffic
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The machine spirit has suffered a critical host configuration crash.");
}
finally
{
    // Ensuring background logging streams dump write buffers safely down to disk paths on close signals
    Log.Information("The machine spirit has been laid to rest safely.");
    Log.CloseAndFlush();
}

//Praise Ommnisiah
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Week3Project.Data;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;

//When all this was wrote i was so high on caffeine that I can barely remember how i did it
/// <summary>
/// Core inventory allocation and fulfillment execution engine.
/// Utilizes a factory pattern to orchestrate isolated database sessions, allowing safe, 
/// thread-safe parallel processing across multi-threaded operations without resource leakage.
/// </summary>
public class FulfillmentService : IFulfillmentService
{
    // Factory utilized to dynamically spin up dedicated DbContext snapshots per execution thread path
    private readonly IDbContextFactory<StoreDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FulfillmentService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The database context factory injected via dependency container.</param>
    public FulfillmentService(IDbContextFactory<StoreDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Processes a massive batch (burst) of incoming order IDs, orchestrating execution path
    /// routing across either a strict serialized timeline or an split multi-lane parallel matrix.
    /// </summary>
    /// <param name="ids">Collection of primary keys matching orders designated for evaluation.</param>
    /// <param name="ctk">Asynchronous termination token tracking upstream application state flags.</param>
    /// <param name="useParallel">Flag determining if multi-threaded execution tracks are utilized.</param>
    public async Task ProcessBurstAsync(IReadOnlyList<int> ids, CancellationToken ctk, bool useParallel = false)
    {
        Log.Information("Planificador iniciado para {Count} órdenes. Modo Paralelo: {Mode}", ids.Count, useParallel);

        List<PurchaseOrder> orders;

        // Open an isolated context state loop simply to collect target data characteristics
        using (var setupContext = await _dbContextFactory.CreateDbContextAsync(ctk))
        {
            orders = await setupContext.Orders
                .Where(o => ids.Contains(o.Id))
                .ToListAsync(ctk);
        }

        if (useParallel)
        {
            // ====================================================================
            // --- TWO-LANE PARALLEL STRATEGY ---
            // ====================================================================
            // Group operations into two distinct scheduling arrays to ensure high-priority
            // Express orders bypass standard shipping backlogs entirely while maximizing execution threads.

            var expeditedIds = orders
                .Where(o => o.Priority == OrderPriority.SpeedPlus)
                .Select(o => o.Id)
                .ToList();

            var normalIds = orders
                .Where(o => o.Priority != OrderPriority.SpeedPlus)
                .Select(o => o.Id)
                .ToList();

            // Lane A: Process all Premium Priority entries concurrently across free system threads
            if (expeditedIds.Any())
            {
                Log.Information("Processing {Count} Expedited orders concurrently...", expeditedIds.Count);

                // Map the ID list directly into individual executable tasks
                var expeditedTasks = expeditedIds.Select(id => FulfillSingleOrderWithRetryAsync(id, ctk));

                // Fork/Join execution pattern: Wait for the entire express lane matrix to fully clear
                await Task.WhenAll(expeditedTasks);
            }

            // Lane B: Process standard entries concurrently ONLY once the primary lane clears.
            // This prevents standard orders from stealing database resource locks from premium tasks.
            if (normalIds.Any())
            {
                Log.Information("Expedited lane cleared. Processing {Count} Normal orders concurrently...",
                    normalIds.Count);

                var normalTasks = normalIds.Select(id => FulfillSingleOrderWithRetryAsync(id, ctk));
                await Task.WhenAll(normalTasks);
            }
        }
        else
        {
            // ====================================================================
            // --- SEQUENTIAL STRATEGY ---
            // ====================================================================
            // Clean single-threaded execution baseline path used during telemetry validation runs.
            var sortedOrderIds = OrderByPriority(orders).ToList();

            foreach (var id in sortedOrderIds)
            {
                // Explicitly check the cancellation token inside the hot loop to support quick aborts
                if (ctk.IsCancellationRequested)
                {
                    Log.Warning("Fulfillment drain interrupted by server shutdown.");
                    ctk.ThrowIfCancellationRequested();
                }

                await FulfillSingleOrderWithRetryAsync(id, ctk);
            }
        }

        Log.Information("Procesamiento completado.");
    }

    /// <summary>
    /// Processes stock deductions and records status audits for a single order.
    /// Implements an optimistic concurrency retry loop to handle row-level conflicts under parallel load.
    /// </summary>
    private async Task FulfillSingleOrderWithRetryAsync(int orderId, CancellationToken ctk)
    {
        const int maxRetries = 3;
        int retry = 0;

        while (retry < maxRetries)
        {
            retry++;

            // Create an isolated context session and start an atomic transaction for this specific order processing lane
            using var context = await _dbContextFactory.CreateDbContextAsync(ctk);
            using var transaction = await context.Database.BeginTransactionAsync(ctk);

            try
            {
                // Deep fetch the complete relational entity graph required to compute allocation logic
                var order = await context.Orders
                    .Include(o => o.OrderLines)
                    .ThenInclude(ol => ol.Card)
                    .ThenInclude(c => c.Inventory)
                    .FirstOrDefaultAsync(o => o.Id == orderId, ctk);

                // Validation Guard: Ensure target order exists in the schema
                if (order == null)
                    return;

                // Idempotency Guard: Skip evaluation if another asynchronous thread already captured and completed this order
                if (order.Status != OrderStatus.Pending)
                    return;

                // Business Logic Analysis: Evaluate if all line item quantities are satisfied by stock levels
                bool hasStock = order.OrderLines.All(line =>
                    line.Card.Inventory != null &&
                    line.Card.Inventory.QuantityOnHand >= line.Quantity);

                string auditMessage;

                if (hasStock)
                {
                    // Deduct allocated balances from the Inventory rows
                    foreach (var line in order.OrderLines)
                    {
                        line.Card.Inventory!.QuantityOnHand -= line.Quantity;
                    }

                    order.Status = OrderStatus.Fulfilled;
                    auditMessage = $"Successfully allocated stock for Order {orderId}.";
                }
                else
                {
                    // Flag order state structure as a Backorder for secondary fulfillment tracks
                    order.Status = OrderStatus.Backordered;
                    auditMessage = $"Insufficient stock. Order {orderId} forced to Backorder.";
                }

                order.CompletedAt = DateTime.UtcNow;

                // Document operation audit entries directly into historical logging arrays
                var auditEvent = new FulFillmentLog()
                {
                    PurchaseOrderId = order.Id,
                    Message = auditMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = "Fulfillment"
                };
                await context.FulfillmentLogs.AddAsync(auditEvent, ctk);

                // Save changes. Concurrency token triggers a DbUpdateConcurrencyException if modified elsewhere
                await context.SaveChangesAsync(ctk);

                // Commit changes safely up to the underlying persistence engine layers
                await transaction.CommitAsync(ctk);

                Log.Information("Result: {Status} | Order ID: {OrderId} (Attempt {Attempt})",
                    order.Status, order.Id, retry);
                return; // Execution succeeded, break the retry loop and exit the execution path safely
            }
            catch (DbUpdateConcurrencyException)
            {
                // Roll back database changes for this execution attempt to clear corrupted transactional footprints
                await transaction.RollbackAsync(ctk);

                Log.Warning("Concurrency collision on Order {OrderId}. Retry attempt {Attempt}/{Max}",
                    orderId, retry, maxRetries);

                // If processing efforts hit the failure limit, execute fallback procedures to clear thread blocks
                if (retry == maxRetries)
                {
                    Log.Error("Max retries exhausted for Order {OrderId}. Forcing to Backordered state.", orderId);

                    // Create an isolated fallback context to execute recovery changes
                    using var fallbackContext = await _dbContextFactory.CreateDbContextAsync(ctk);
                    var fallbackOrder = await fallbackContext.Orders.FindAsync(new object[] { orderId }, ctk);

                    if (fallbackOrder != null && fallbackOrder.Status == OrderStatus.Pending)
                    {
                        fallbackOrder.Status = OrderStatus.Backordered;
                        fallbackOrder.CompletedAt = DateTime.UtcNow;

                        var fallbackAudit = new FulFillmentLog()
                        {
                            PurchaseOrderId = orderId,
                            Message = "Forced to backorder due to excessive concurrency collisions.",
                            Timestamp = DateTime.UtcNow,
                            Type = "Fulfillment"
                        };
                        await fallbackContext.FulfillmentLogs.AddAsync(fallbackAudit, ctk);
                        await fallbackContext.SaveChangesAsync(ctk);
                    }

                    return; // Fail gracefully out of execution blocks
                }

                // Linear jitter mitigation pattern: Introduce a random millisecond delay 
                // to spread out thread execution times and reduce immediate table deadlocks.
                await Task.Delay(Random.Shared.Next(10, 40), ctk);
            }
        }
    }

    /// <summary>
    /// Helper calculation sorting order arrays by business priority characteristics sequentially.
    /// </summary>
    private static IEnumerable<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        return orders
            .OrderByDescending(o => o.Priority == OrderPriority.SpeedPlus)
            .Select(o => o.Id);
    }

    // Dos colas concurrentes (SeedPlus primero)
    //Este se me presento como una epifania, aun no esta testeado pero aqui esta
    //Miralo que bonito
    public async Task MicroPlasticBurstAsync(IReadOnlyList<int> ids, CancellationToken cts)
    {
        var speedPlusQueue = new ConcurrentQueue<int>();
        var speedQueue = new ConcurrentQueue<int>();

        var db = _dbContextFactory.CreateDbContext();
        
        var orders = await db.Orders
                .Where(o => ids.Contains(o.Id))
                .Select(o => new { o.Id, o.Priority })
                .ToListAsync(cts);

            foreach (var order in orders)
            {
                if (order.Priority == OrderPriority.SpeedPlus)
                    speedPlusQueue.Enqueue(order.Id);
                else
                    speedQueue.Enqueue(order.Id);
            }

        async Task Worker()
        {
            while (!cts.IsCancellationRequested)
            {
                if (speedPlusQueue.TryDequeue(out var orderId))
                {
                    await FulfillSingleOrderWithRetryAsync(orderId, cts);
                }
                else if (speedQueue.TryDequeue(out orderId))
                {
                    await FulfillSingleOrderWithRetryAsync(orderId, cts);
                }
                else
                {
                    break;
                }
            }
        }

        int workerCount = Environment.ProcessorCount;

        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(Worker, cts));

        await Task.WhenAll(workers);

        Log.Information("Ráfaga procesada con doble cola concurrente.");
    }
}
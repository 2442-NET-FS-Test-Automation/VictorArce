using Microsoft.EntityFrameworkCore;
using Serilog;
using Week3Project.Data;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;

//When all this was wrote i was so high on caffeine that I can barely remember how i did it
public class FulfillmentService : IFulfillmentService
{
private readonly IDbContextFactory<StoreDbContext> _dbContextFactory;

    public FulfillmentService(IDbContextFactory<StoreDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    public async Task ProcessBurstAsync(IReadOnlyList<int> ids, CancellationToken ctk, bool useParallel = false)
    {
        Log.Information("Planificador iniciado para {Count} órdenes. Modo Paralelo: {Mode}", ids.Count, useParallel);

        List<PurchaseOrder> orders;

        using (var setupContext = await _dbContextFactory.CreateDbContextAsync(ctk))
        {
            orders = await setupContext.Orders
                .Where(o => ids.Contains(o.Id))
                .ToListAsync(ctk);
        }

        if (useParallel)
        {
            // --- TWO-LANE PARALLEL STRATEGY (Maintains priority inside a burst) ---
            var expeditedIds = orders
                .Where(o => o.Priority == OrderPriority.SpeedPlus)
                .Select(o => o.Id)
                .ToList();

            var normalIds = orders
                .Where(o => o.Priority != OrderPriority.SpeedPlus)
                .Select(o => o.Id)
                .ToList();

            // Lane A: Process all Expedited orders concurrently
            if (expeditedIds.Any())
            {
                Log.Information("Processing {Count} Expedited orders concurrently...", expeditedIds.Count);
                var expeditedTasks = expeditedIds.Select(id => FulfillSingleOrderWithRetryAsync(id, ctk));
                await Task.WhenAll(expeditedTasks);
            }

            // Lane B: Process all Normal orders concurrently ONLY after expedited lane clears[cite: 1]
            if (normalIds.Any())
            {
                Log.Information("Expedited lane cleared. Processing {Count} Normal orders concurrently...", normalIds.Count);
                var normalTasks = normalIds.Select(id => FulfillSingleOrderWithRetryAsync(id, ctk));
                await Task.WhenAll(normalTasks);
            }
        }
        else
        {
            // --- SEQUENTIAL STRATEGY (Your original clean baseline run)[cite: 1] ---
            var sortedOrderIds = OrderByPriority(orders).ToList();

            foreach (var id in sortedOrderIds)
            {
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
    
    // Individual processing with concurrency retries
    private async Task FulfillSingleOrderWithRetryAsync(int orderId, CancellationToken ctk)
    {
        const int maxRetries = 3;
        int retry = 0;

        while (retry < maxRetries)
        {
            retry++;

            using var context = await _dbContextFactory.CreateDbContextAsync(ctk);
            using var transaction = await context.Database.BeginTransactionAsync(ctk);

            try
            {
                var order = await context.Orders
                    .Include(o => o.OrderLines)
                        .ThenInclude(ol => ol.Card)
                        .ThenInclude(c => c.Inventory)
                    .FirstOrDefaultAsync(o => o.Id == orderId, ctk);

                if (order == null)
                    return;

                // Skip processing if the order is already handled
                if (order.Status != OrderStatus.Pending) 
                    return;

                bool hasStock = order.OrderLines.All(line =>
                    line.Card.Inventory != null &&
                    line.Card.Inventory.QuantityOnHand >= line.Quantity);

                string auditMessage;

                if (hasStock)
                {
                    foreach (var line in order.OrderLines)
                    {
                        line.Card.Inventory!.QuantityOnHand -= line.Quantity;
                    }

                    order.Status = OrderStatus.Fulfilled;
                    auditMessage = $"Successfully allocated stock for Order {orderId}.";
                }
                else
                {
                    order.Status = OrderStatus.Backordered;
                    auditMessage = $"Insufficient stock. Order {orderId} forced to Backorder.";
                }

                order.CompletedAt = DateTime.UtcNow;

                // Keeps your exact audit log entity & context collection![cite: 1]
                var auditEvent = new FulFillmentLog()
                {
                    PurchaseOrderId = order.Id,
                    Message = auditMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = "Fulfillment"
                };
                await context.FulfillmentLogs.AddAsync(auditEvent, ctk);

                // Save changes. Concurrency token triggers DbUpdateConcurrencyException if modified elsewhere[cite: 1]
                await context.SaveChangesAsync(ctk);
                await transaction.CommitAsync(ctk);

                Log.Information("Result: {Status} | Order ID: {OrderId} (Attempt {Attempt})", 
                    order.Status, order.Id, retry);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ctk);

                Log.Warning("Concurrency collision on Order {OrderId}. Retry attempt {Attempt}/{Max}", 
                    orderId, retry, maxRetries);

                if (retry == maxRetries)
                {
                    Log.Error("Max retries exhausted for Order {OrderId}. Forcing to Backordered state.", orderId);
                    
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

                    return;
                }

                // Smooth jittered delay to let database rows settle[cite: 1]
                await Task.Delay(Random.Shared.Next(10, 40), ctk);
            }
        }
    }

    private static IEnumerable<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        return orders
            .OrderByDescending(o => o.Priority == OrderPriority.SpeedPlus)
            .Select(o => o.Id);
    }
}

    // Dos colas concurrentes (SeepPlus primero)
    //Este se me presento como una epifania, aun no esta testeado pero aqui esta
    //Miralo que bonito
    /*
    public async Task MicroPlasticBurstAsync(IReadOnlyList<int> ids, CancellationToken cts)
    {
        var speedPlusQueue = new ConcurrentQueue<int>();
        var speedQueue = new ConcurrentQueue<int>();

        var db = _dbContextFactory
        {
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
    */ 
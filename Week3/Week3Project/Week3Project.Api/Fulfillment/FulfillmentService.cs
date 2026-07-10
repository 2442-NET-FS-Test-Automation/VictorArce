using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Week3Project.Data;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;

public class FulfillmentService : IFulfillmentService
{
    private readonly IDbContextFactory<StoreDbContext> _dbContextFactory;

    public FulfillmentService(IDbContextFactory<StoreDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    // Estrategia 1: Procesamiento por prioridad usando OrderBy
    public async Task ProcessBurstAsync(IReadOnlyList<int> orderIds, CancellationToken cts)
    {
        Log.Information("Planificador iniciado para {Count} órdenes.", orderIds.Count);

        List<int> sortedOrderIds;

        using (var setupContext = await _dbContextFactory.CreateDbContextAsync(cts))
        {
            var orders = await setupContext.Orders
                .Where(o => orderIds.Contains(o.Id))
                .ToListAsync(cts);

            sortedOrderIds = OrderByPriority(orders).ToList();
        }

        var tasks = sortedOrderIds
            .Select(id => FulfillSingleOrderWithRetryAsync(id, cts));

        await Task.WhenAll(tasks);

        Log.Information("Procesamiento completado.");
    }

    // Estrategia 2: Dos colas concurrentes (Expedited primero)
    public async Task MicroPlasticBurstAsync(IReadOnlyList<int> ids, CancellationToken cts)
    {
        var expeditedQueue = new ConcurrentQueue<int>();
        var normalQueue = new ConcurrentQueue<int>();

        using (var setupContext = await _dbContextFactory.CreateDbContextAsync(cts))
        {
            var orders = await setupContext.Orders
                .Where(o => ids.Contains(o.Id))
                .Select(o => new { o.Id, o.Priority })
                .ToListAsync(cts);

            foreach (var order in orders)
            {
                if (order.Priority == OrderPriority.Expedited)
                    expeditedQueue.Enqueue(order.Id);
                else
                    normalQueue.Enqueue(order.Id);
            }
        }

        async Task Worker()
        {
            while (!cts.IsCancellationRequested)
            {
                if (expeditedQueue.TryDequeue(out var orderId))
                {
                    await FulfillSingleOrderWithRetryAsync(orderId, cts);
                }
                else if (normalQueue.TryDequeue(out orderId))
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

    // Procesamiento individual con reintentos por concurrencia
    private async Task FulfillSingleOrderWithRetryAsync(int orderId, CancellationToken cts)
    {
        const int maxRetries = 3;
        int retry = 0;

        while (retry < maxRetries)
        {
            retry++;

            using var context = await _dbContextFactory.CreateDbContextAsync(cts);
            using var transaction = await context.Database.BeginTransactionAsync(cts);

            try
            {
                var order = await context.Orders
                    .Include(o => o.OrderLines)
                        .ThenInclude(ol => ol.Card)
                        .ThenInclude(c => c!.Inventory)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cts);

                if (order == null)
                    return;

                bool hasStock = order.OrderLines.All(line =>
                    line.Card.Inventory != null &&
                    line.Card.Inventory.QuantityOnHand >= line.Quantity);

                if (hasStock)
                {
                    foreach (var line in order.OrderLines)
                    {
                        line.Card.Inventory!.QuantityOnHand -= line.Quantity;
                    }

                    order.Status = OrderStatus.Fulfilled;
                }
                else
                {
                    order.Status = OrderStatus.Backordered;
                }

                order.CompletedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(cts);
                await transaction.CommitAsync(cts);

                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cts);

                if (retry == maxRetries)
                {
                    using var fallbackContext =
                        await _dbContextFactory.CreateDbContextAsync(cts);

                    var fallbackOrder =
                        await fallbackContext.Orders.FindAsync(new object[] { orderId }, cts);

                    if (fallbackOrder != null)
                    {
                        fallbackOrder.Status = OrderStatus.Backordered;
                        fallbackOrder.CompletedAt = DateTime.UtcNow;

                        await fallbackContext.SaveChangesAsync(cts);
                    }

                    return;
                }

                await Task.Delay(Random.Shared.Next(10, 40), cts);
            }
        }
    }

    // Ordena primero las órdenes Expedited
    private static IEnumerable<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        return orders
            .OrderByDescending(o => o.Priority == OrderPriority.Expedited)
            .Select(o => o.Id);
    }
}
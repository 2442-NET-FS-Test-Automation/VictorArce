using Week3Project.Data.Entities;
using Week3Project.Data;
using Microsoft.EntityFrameworkCore;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Seeder;

public class Seeder : ISeeder
{
    private static readonly string[] cards = { "YGO-GAOV-ENSP1-UR", "YGO-SDK-EN001-UR", "YGO-BPRO-EN013-SR", "YGO-GAOV-EN000-SR"};
    
    private readonly IDbContextFactory<StoreDbContext> _dbContextFactory;

    public Seeder(IDbContextFactory<StoreDbContext> factory)
    {
        _dbContextFactory = factory;
    }

    public IReadOnlyList<int> Seed(int numOrders, bool expedited)
    {
        var db = _dbContextFactory.CreateDbContext();
        
        var pid = db.Cards.ToDictionary(c => c.Sku, c => c.Id);

        var ids = new List<int>(numOrders);

        for (int i = 0; i < numOrders; i++)
        {
            var order = new PurchaseOrder
            {
                CustomerId = Random.Shared.Next(1, 3),
                Priority = expedited ? OrderPriority.Expedited : OrderPriority.Normal,
                OrderLines = { new OrderLine { CardId = pid[cards[i % cards.Length]], Quantity = 1 } }
            };
            db.Orders.Add(order);
            db.SaveChanges();
            ids.Add(order.Id);
        }
        return ids;
    }
}
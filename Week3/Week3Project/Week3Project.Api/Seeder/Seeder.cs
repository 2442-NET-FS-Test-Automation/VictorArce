using System;
using System.Collections.Generic;
using System.Linq;
using Week3Project.Data.Entities;
using Week3Project.Data;
using Microsoft.EntityFrameworkCore;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Seeder;

public class Seeder : ISeeder
{
    private readonly IDbContextFactory<StoreDbContext> _dbContextFactory;

    public Seeder(IDbContextFactory<StoreDbContext> factory)
    {
        _dbContextFactory = factory;
    }

    // Seeding the database with random data
    //This thing should be called heretic for ridicoulos amount of interactions it does
    public IReadOnlyList<int> Seed(int numOrders, bool priority)
    {
        using var db = _dbContextFactory.CreateDbContext();
    
        // Maping of SKU -> ID of the DB
        var pid = db.Cards.ToDictionary(c => c.Sku, c => c.Id);
    
        // Convertimos las llaves (los SKUs strings) en una lista para poder seleccionarlas por índice numérico
        //We convert the keys (the SKU strings) into a list so we can select them by numeric index
        var skusDisponibles = pid.Keys.ToList();
        int cantidadDeCartas = skusDisponibles.Count;
    
        List<int> ids = new();

        for (int i = 0; i < numOrders; i++)
        {
            // 1. Select a random SKU from the list of strings
            string skuAleatorio = skusDisponibles[Random.Shared.Next(0, cantidadDeCartas)];
            // 2. Get the real Id using the dictionary
            int cardIdReal = pid[skuAleatorio];

            //I wanted to build a full order for the test just to be sure
            var order = new PurchaseOrder
            {
                // Unique: (1, 4) generates 1, 2 and 3, important because we seeded 3 customers
                CustomerId = Random.Shared.Next(1, 4), 
                Priority = priority ? OrderPriority.Expedited : OrderPriority.Normal,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            
                // Initialize
                OrderLines = new List<OrderLine> 
                { 
                    new OrderLine 
                    { 
                        CardId = cardIdReal, 
                        Quantity = 1 
                    } 
                }
            };
        
            db.Orders.Add(order);
            // So, we'll add the ID to the list later when the SaveChanges() is called.'
        }
        
        //Save all the orders in a single batch
        db.SaveChanges();
    
        // Una vez ejecutado el SaveChanges, los IDs reales ya existen en memoria gracias al ChangeTracker
        foreach (var entry in db.ChangeTracker.Entries<PurchaseOrder>())
        {
            ids.Add(entry.Entity.Id);
        }
    
        return ids;
    }
    
    //So, I know each card has a different stock but it's easier to just set it to 10 and call it a day
    public void Restock()
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.Cards.ToList().ForEach(c => c.Inventory.QuantityOnHand = 10);
    }
    
    //Easy as it reads, set all the stock to 0
    public void Clear()
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.Cards.ToList().ForEach(c => c.Inventory.QuantityOnHand = 0);
    }

    //Set stock to desired 
    public void StockToTarget(int i)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.Cards.ToList().ForEach(c => c.Inventory.QuantityOnHand = i);
    }
}
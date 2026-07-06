using Microsoft.EntityFrameworkCore;
using Week3Project.Data.Entities;

namespace Week3Project.Data;

public class StoreDbContext : DbContext
{
    // Constructor (never used directly)
    public StoreDbContext(DbContextOptions<StoreDbContext> options) : 
    base(options)
    {
    }

    // Database Tables
    //Remember the structure of this
    //DBset <Entity> = Set<Entity>
    //Where Entity is the name of the table in the DB and a class in the code
    public DbSet<Customer> Customer => Set<Customer>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardInventory> Inventories => Set<CardInventory>();
    public DbSet<PurchaseOrder> Orders => Set<PurchaseOrder>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<FulFillmentLog> FulfillmentLogs => Set<FulFillmentLog>();
    
    
    //This is where we configure our database schema and constraints (indexes, etc.)
    //This is not necessary thanks to our Entity Framework Core migrations,
    //but it's good to keep it here', just in case
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Unique Index on Customer Email
        modelBuilder.Entity<Customer>()
            .HasIndex(p => p.Email)
            .IsUnique();

        // 2. Unique Index on Card SKU
        modelBuilder.Entity<Card>()
            .HasIndex(c => c.Sku)
            .IsUnique();
        
        //Precise Decimal Pricing
        modelBuilder.Entity<Card>()
            .Property(c => c.Price)
            .HasColumnType("decimal(10,2)");

        // 3. 1:1 Relationship & RowVersion Concurrency Token configuration
        modelBuilder.Entity<CardInventory>()
            .HasOne(i => i.Card)
            .WithOne(c => c.Inventory)
            .HasForeignKey<CardInventory>(i => i.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CardInventory>()
            .Property(i => i.RowVersion)
            .IsRowVersion(); // Automatically configures column as a byte[] timestamp concurrency token
                             // this is the most confusing thing ive seen in C#, at least for now

        // 4. Non-Key Index on Order Status for rapid scanning & processing
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(o => o.Status);

        // 5. Enums converted to Strings in the DB for readable reporting / direct SQL queries
        modelBuilder.Entity<PurchaseOrder>()
            .Property(o => o.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();
        
        // 6. Timestamps for auditing
        modelBuilder.Entity<FulFillmentLog>()
            .Property(l => l.Timestamp)
            .HasColumnType("datetime2");
        
        //Let's get some cards to fill the DB
        //Just a few because we are just testing
        modelBuilder.Entity<Card>().HasData(
            
            new Card {Id = 1, Sku = "YGO-BPRO-EN013-SR", Name = "Elfnote Power Patron", Price = 1.99m},
            new Card {Id = 2, Sku = "YGO-SDK-EN001-UR", Name = "Blue-Eyes White Dragon", Price = 40.00m},
            //Sneak peek Artorigus
            new Card {Id = 3, Sku = "YGO-GAOV-ENSP1-UR", Name = "Noble Knight Artorigus", Price = 4.00m},
            //Regular booster Artorigus
            new Card {Id = 4, Sku = "YGO-GAOV-EN000-SR", Name = "Noble Knight Artorigus", Price = 0.99m});
        //Our SKU goes this way
        //YGO = The TCG we are working on
        //GAOV = Card Set
        //EN000 =language and Card Number in that set (SP1 is for sneak peek, three numbers is for regular booster)
        //UR = Card Rarity (UR = Ultra Rare, SP = Super Rare)
        
        
        //Some inventory, remember to set the CardId to the Id of the Card
        modelBuilder.Entity<CardInventory>().HasData(
            new CardInventory {Id = 1, CardId = 1, QuantityOnHand = 20},
            new CardInventory {Id = 2, CardId = 2, QuantityOnHand = 0},
            new CardInventory {Id = 3, CardId = 3, QuantityOnHand = 3},
            new CardInventory {Id = 4, CardId = 4, QuantityOnHand = 8});

        //Some customers
        modelBuilder.Entity<Customer>().HasData(
            new Customer{Id = 1, Name = "Genaro", Email = "genSal@tmp.com"},
            new Customer{Id = 2, Name = "Brandon", Email = "bran@tmp.com"},
            new Customer{Id = 3, Name = "Razen", Email = "vsStart@tmp.com"}    
            );
    }
    
    //I don't like databases, but I like code.
}
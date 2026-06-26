using Microsoft.EntityFrameworkCore;
using Library.Data.Entities;

namespace Library.Data;

public class LibraryDBContext : DbContext
{
    //This class needs a constructor, and it needs to take a certain argument
    //we ourselves will never call this constructor, ASP.NET DI container will do it for us
    public LibraryDBContext(DbContextOptions<LibraryDBContext> options) : base(options){}
    
    //We need to tell our DBContext what C# classes we are tracking as Entities
    //Reminder - these entities become our tables
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLines> OrderLines => Set<OrderLines>();
    //Aside from previous declarations we can alsl incllude entities like this
    public DbSet<FulfillmentEvent> FulfillmentEvents {get; set; }

    //If i wat to do things like deeper configuration options or data seeding 
    //I can override a method we inherited from dbcontext
    //called OnModelCreating() - this is called when EF Core creates a migration

    protected override void OnModelCreating(ModelBuilder b)
    {
        //I can set anytthing i want as far as constraints, mapping column names and types 
        //inside of here using something called Fluent API. Ef Core Lets you do config
        //in 3 ways. convention < Data Annotations < Fluent API in OnModelCreating()

        //for example here is the same config we did by convention and annotation prior
        b.Entity<Product>
        (e =>
        {
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.HasOne(p=> p.Intentory)
                .WithOne(i => i.Product)
                .HasForeignKey<InventoryItem>(i => i.ProductId);
        });

        //Settig our row version property as an EF Core row version
        b.Entity<InventoryItem>().Property(i => i.RowVersion).IsRowVersion();

        //This order of operations, setting string length and then telling DB that
        //a column is unique is specific to string + sql server
        b.Entity<Customer>().Property(c => c.Email).HasMaxLength(256);
        b.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

        //After youve configured your entities
        //We can use OnModelCreating to seed data
        b.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Sku = "A100",
                Name = "Product A",
                Price = 9.99m
            },
            new Product
            {
                Id = 2,
                Sku = "B200",
                Name = "Product B",
                Price = 19.99m
            },
            new Product
            {
                Id = 3,
                Sku = "C300",
                Name = "Product C",
                Price = 29.99m
            }
        );

        //Has data can be used to seed any entity,
        //It runs before SQL server can hand out identity keys
        //Wich is why we give explicit PK when seeding
        b.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                Name = "John Doe",
                Email = "JDoe@example.com"
            },
            new Customer
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "JS@example.com"
            }
        );
    }
}
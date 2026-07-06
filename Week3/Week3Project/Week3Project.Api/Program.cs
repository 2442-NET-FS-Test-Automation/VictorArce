using Microsoft.EntityFrameworkCore;
using Week3Project.Data;


var builder = WebApplication.CreateBuilder(args);

//String for connection to the server, IT SHOUlD BE ON A .env FILE AND NOT HERE!
//It's already moved to appsetings.json but, I'll leave the note here
//var for the DB connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//Connection to SQL server
//Add the DB context
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

//Here goes the swagger stuff
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World!");

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

//Create - Post
// app.MapPost("/cards", async (Card newCard, StoreDbContext db) =>
// {
//     db.Cards.Add(newCard);
//     await db.SaveChangesAsync();
//     return Results.Created($"/cards/{newCard.Id}", newCard);
// });

//Read - Get


//Update - Put

//Delete - Delete


//app.Run() always has to be at the end of the file either on a minimal API or a Controller Api
app.Run();

//Praise Ommnisiah
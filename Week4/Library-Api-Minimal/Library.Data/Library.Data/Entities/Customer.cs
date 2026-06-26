using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library.Data.Entities;

[Table("Customers")]
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = default;
    public string Email { get; set; } = default;
    public List<Order> Orders { get; set; } = new();
}
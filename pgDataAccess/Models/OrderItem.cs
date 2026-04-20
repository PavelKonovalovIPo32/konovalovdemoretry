using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pgDataAccess.Models;

public class OrderItem
{
    public int OrderId { get; set; }
    public string ProductArticle { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}

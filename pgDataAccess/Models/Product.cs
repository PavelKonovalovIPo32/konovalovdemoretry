using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pgDataAccess.Models;

public class Product
{
    [Key]
    public string Article { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт.";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public int ManufacturerId { get; set; }
    public int SupplierId { get; set; }
    public int DiscountPercent { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public string? PhotoPath { get; set; }

    public Category? Category { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [NotMapped]
    public decimal PriceWithDiscount => Price * (1 - DiscountPercent / 100m);

    // Вычисляемые свойства для удобного отображения в UI
    [NotMapped]
    public string CategoryName => Category?.Name ?? "—";

    [NotMapped]
    public string ManufacturerName => Manufacturer?.Name ?? "—";

    [NotMapped]
    public string SupplierName => Supplier?.Name ?? "—";
}

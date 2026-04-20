namespace pgDataAccess.Models;

using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int PickupPointId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = "Новый";
    public int PickupCode { get; set; }

    public User? Client { get; set; }
    public PickupPoint? PickupPoint { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Вычисляемое свойство для отображения суммы заказа
    [NotMapped]
    public decimal Total
    {
        get
        {
            decimal total = 0;
            foreach (var item in OrderItems)
            {
                if (item.Product != null)
                {
                    var price = item.Product.Price * (1 - item.Product.DiscountPercent / 100m);
                    total += price * item.Quantity;
                }
            }
            return total;
        }
    }
}

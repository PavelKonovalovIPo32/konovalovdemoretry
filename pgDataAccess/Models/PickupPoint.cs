namespace pgDataAccess.Models;

public class PickupPoint
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

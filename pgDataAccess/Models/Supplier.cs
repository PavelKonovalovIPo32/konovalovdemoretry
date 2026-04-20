namespace pgDataAccess.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

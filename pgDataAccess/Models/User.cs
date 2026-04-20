namespace pgDataAccess.Models;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }

    public Role? Role { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

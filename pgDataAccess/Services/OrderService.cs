using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;

namespace pgDataAccess.Services;

public class OrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(Order order)
    {
        try
        {
            order.PickupCode = GeneratePickupCode();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new Exception(
                "Ошибка при добавлении заказа:\n" +
                (ex.InnerException?.Message ?? ex.Message),
                ex);
        }
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (order != null)
        {
            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Order>> GetByClientIdAsync(int clientId)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<List<Order>> GetByStatusAsync(string status)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.Status == status)
            .ToListAsync();
    }

    public int GeneratePickupCode()
    {
        var random = new Random();
        int code;

        do
        {
            code = random.Next(1000, 9999);
        }
        while (_context.Orders.Any(o => o.PickupCode == code));

        return code;
    }

    public decimal CalculateTotal(Order order)
    {
        decimal total = 0;
        foreach (var item in order.OrderItems)
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

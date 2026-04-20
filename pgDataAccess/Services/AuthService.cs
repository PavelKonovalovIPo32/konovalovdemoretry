using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;

namespace pgDataAccess.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> AuthenticateAsync(string login, string passwordHash)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == login && u.PasswordHash == passwordHash);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .ToListAsync();
    }

    public Task<bool> IsAdminAsync(User user)
    {
        return Task.FromResult(user?.Role?.Name == "Администратор");
    }

    public Task<bool> IsManagerAsync(User user)
    {
        return Task.FromResult(user?.Role?.Name == "Менеджер");
    }

    public Task<bool> IsClientAsync(User user)
    {
        return Task.FromResult(user?.Role?.Name == "Авторизированный клиент");
    }
}

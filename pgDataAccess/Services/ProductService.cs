using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;

namespace pgDataAccess.Services;

public class ProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Manufacturer)
            .Include(p => p.Supplier)
            .ToListAsync();
    }

    public async Task<Product?> GetByArticleAsync(string article)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Manufacturer)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Article == article);
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string article)
    {
        var product = await _context.Products.FindAsync(article);
        if (product != null)
        {
            // Удаляем связанные элементы заказа
            var orderItems = await _context.OrderItems.Where(oi => oi.ProductArticle == article).ToListAsync();
            _context.OrderItems.RemoveRange(orderItems);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Manufacturer)
            .Include(p => p.Supplier)
            .Where(p => p.Name.Contains(searchTerm) || p.Article.Contains(searchTerm) || p.Description!.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Manufacturer)
            .Include(p => p.Supplier)
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();
    }

    public decimal GetPriceWithDiscount(Product product)
    {
        return product.Price * (1 - product.DiscountPercent / 100m);
    }
}

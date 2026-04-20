using System.Data.Common;
using System.Globalization;
using Npgsql;
using pgDataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace pgDataAccess.Services;

public class ImportService
{
    private static ApplicationDbContext _context;
    

    public ImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ImportAllAsync(string importsFolder)
    {
        var appConnStr = DatabaseConfig.ConnectionString;

        // Подключаемся через app
        _context.ConnectionString = appConnStr;
        await _context.Database.EnsureCreatedAsync();

        await ImportRolesAsync(Path.Combine(importsFolder, "roles.csv"));
        await ImportUsersAsync(Path.Combine(importsFolder, "users.csv"));
        await ImportCategoriesAsync(Path.Combine(importsFolder, "categories.csv"));
        await ImportManufacturersAsync(Path.Combine(importsFolder, "manufacturers.csv"));
        await ImportSuppliersAsync(Path.Combine(importsFolder, "suppliers.csv"));
        await ImportPickupPointsAsync(Path.Combine(importsFolder, "pickup_points.csv"));
        await ImportProductsAsync(Path.Combine(importsFolder, "products.csv"));
        await ImportOrdersAsync(Path.Combine(importsFolder, "orders.csv"));
        await ImportOrderItemsAsync(Path.Combine(importsFolder, "order_items.csv"));
        
        await SyncOrderSequenceAsync();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new Exception(
                "Ошибка при сохранении в БД:\n" +
                (ex.InnerException?.Message ?? ex.Message),
                ex);
        }
    }
    public async Task SyncOrderSequenceAsync()
    {
        var sql = @"
        SELECT setval(
            pg_get_serial_sequence('""Orders""', 'Id'),
            COALESCE((SELECT MAX(""Id"") FROM ""Orders""), 1)
        );
    ";

        await using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection?)_context.Database.GetDbConnection());

        if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            await _context.Database.OpenConnectionAsync();

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ImportRolesAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Roles.Any(r => r.Id == id))
                {
                    _context.Roles.Add(new Role { Id = id, Name = parts[1] });
                }
            }
        }
    }

    private async Task ImportUsersAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 5 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Users.Any(u => u.Id == id))
                {
                    _context.Users.Add(new User
                    {
                        Id = id,
                        Login = parts[1],
                        PasswordHash = parts[2],
                        FullName = parts[3],
                        RoleId = int.Parse(parts[4])
                    });
                }
            }
        }
    }

    private async Task ImportCategoriesAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Categories.Any(c => c.Id == id))
                {
                    _context.Categories.Add(new Category { Id = id, Name = parts[1] });
                }
            }
        }
    }

    private async Task ImportManufacturersAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Manufacturers.Any(m => m.Id == id))
                {
                    _context.Manufacturers.Add(new Manufacturer { Id = id, Name = parts[1] });
                }
            }
        }
    }

    private async Task ImportSuppliersAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Suppliers.Any(s => s.Id == id))
                {
                    _context.Suppliers.Add(new Supplier { Id = id, Name = parts[1] });
                }
            }
        }
    }

    private async Task ImportPickupPointsAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLineComplex(lines[i]);
            if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
            {
                if (!_context.PickupPoints.Any(p => p.Id == id))
                {
                    _context.PickupPoints.Add(new PickupPoint { Id = id, Address = parts[1] });
                }
            }
        }
    }

    private async Task ImportProductsAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLineComplex(lines[i]);
            if (parts.Length >= 11)
            {
                string article = parts[0];
                if (!_context.Products.Any(p => p.Article == article))
                {
                    _context.Products.Add(new Product
                    {
                        Article = article,
                        Name = parts[1],
                        Unit = parts[2],
                        Price = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
                        CategoryId = int.Parse(parts[4]),
                        ManufacturerId = int.Parse(parts[5]),
                        SupplierId = int.Parse(parts[6]),
                        DiscountPercent = int.Parse(parts[7]),
                        StockQuantity = int.Parse(parts[8]),
                        Description = parts[9],
                        PhotoPath = parts[10]
                    });
                }
            }
        }
    }

    private async Task ImportOrdersAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 7 && int.TryParse(parts[0], out int id))
            {
                if (!_context.Orders.Any(o => o.Id == id))
                {
                    _context.Orders.Add(new Order
                    {
                        Id = id,
                        ClientId = int.Parse(parts[1]),
                        PickupPointId = int.Parse(parts[2]),
                        OrderDate = DateTimeOffset.Parse(parts[3]).UtcDateTime,
                        DeliveryDate = DateTimeOffset.Parse(parts[4]).UtcDateTime,
                        Status = parts[5],
                        PickupCode = int.Parse(parts[6])
                    });
                }
            }
        }
    }

    private async Task ImportOrderItemsAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        
        var lines = await File.ReadAllLinesAsync(filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = ParseCsvLine(lines[i]);
            if (parts.Length >= 3)
            {
                var key = new { OrderId = int.Parse(parts[0]), ProductArticle = parts[1] };
                if (!_context.OrderItems.Any(oi => oi.OrderId == key.OrderId && oi.ProductArticle == key.ProductArticle))
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = key.OrderId,
                        ProductArticle = key.ProductArticle,
                        Quantity = int.Parse(parts[2])
                    });
                }
            }
        }
    }

    private string[] ParseCsvLine(string line)
    {
        return line.Split(',');
    }

    private string[] ParseCsvLineComplex(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                // Не добавляем кавычки в результат
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}

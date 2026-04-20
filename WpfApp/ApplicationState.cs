namespace WpfApp;

/// <summary>
/// Глобальное состояние приложения. Без авторизации — полный доступ.
/// </summary>
public static class ApplicationState
{
    public static pgDataAccess.ApplicationDbContext DbContext { get; } = new();

    // Все сервисы используют один и тот же контекст
    public static pgDataAccess.Services.ProductService ProductService => new(DbContext);
    public static pgDataAccess.Services.OrderService OrderService => new(DbContext);
    public static pgDataAccess.Services.ImportService ImportService => new(DbContext);

    public static async Task InitializeDatabaseAsync()
    {
        // Создаём таблицы если не существуют
        await DbContext.Database.EnsureCreatedAsync();
    }
}

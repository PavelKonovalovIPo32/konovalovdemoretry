namespace pgDataAccess;

/// <summary>
/// </summary>
public static class DatabaseConfig
{
  
    public const string DatabaseName = "konovalov";

    public const string Host = "localhost";
    public const int Port = 5432;
    public const string Username = "app";
    public const string Password = "123456789";

    public static string ConnectionString =>
        $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";

    public static string MasterConnectionString =>
        $"Host={Host};Port={Port};Database=postgres;Username=postgres;Password=postgres";
}

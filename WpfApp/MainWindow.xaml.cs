using Microsoft.EntityFrameworkCore;
using Npgsql;
using pgDataAccess.Models;
using pgDataAccess.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace WpfApp;

public partial class MainWindow : Window
{
    private readonly ProductService _productService;
    private readonly OrderService _orderService;
    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private ProductWindow? _productWindow;
    private OrderWindow? _orderWindow;
    private DictionaryWindow? _dictionaryWindow;

    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        var dbName = "konovalov";

        using var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=postgres;Database=postgres");
        conn.Open();

        // 1. Проверка роли + создание
        using (var roleCmd = new NpgsqlCommand(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app') THEN
        CREATE ROLE app WITH LOGIN PASSWORD '123456789' CREATEDB;
    END IF;
END
$$;", conn))
        {
            roleCmd.ExecuteNonQuery();
        }

        // 2. Проверка базы (без string interpolation)
        bool dbExists;

        using (var checkDbCmd = new NpgsqlCommand(@"
SELECT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = @name
);", conn))
        {
            checkDbCmd.Parameters.AddWithValue("name", dbName);
            dbExists = (bool)checkDbCmd.ExecuteScalar()!;
        }

        // 3. Создание базы (если нет)
        if (!dbExists)
        {
            using var createDbCmd = new NpgsqlCommand(
                $@"CREATE DATABASE ""{dbName}"" WITH OWNER app", conn);

            createDbCmd.ExecuteNonQuery();
        }

        _productService = ApplicationState.ProductService;
        _orderService = ApplicationState.OrderService;

        Loaded += MainWindow_Loaded;
    }
    private readonly ImportService _importService =
    new ImportService(ApplicationState.DbContext);


    public async Task ImportCon(string path)
    {
        await _importService.ImportAllAsync(path);
    }
    private void SetWindowIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "Icon.png");
        if (File.Exists(iconPath))
        {
            try
            {
                this.Icon = new BitmapImage(new Uri(iconPath));
            }
            catch { }
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ImportCon("Docs/Imports");
        await LoadProductsAsync();
        await LoadSuppliersAsync();
        await LoadPickupPointsAsync();
        SetupFullAccess();
    }

    /// <summary>
    /// Без авторизации — полный доступ ко всему функционалу.
    /// </summary>
    private void SetupFullAccess()
    {
        UserTextBlock.Text = "Администратор";
        ControlPanel.Visibility = Visibility.Visible;
        AddProductButton.Visibility = Visibility.Visible;
        EditProductButton.Visibility = Visibility.Visible;
        DeleteProductButton.Visibility = Visibility.Visible;
        OrdersButton.Visibility = Visibility.Visible;
        DictionaryButton.Visibility = Visibility.Visible;
        Title = "Обувной магазин - Товары (Администратор)";
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = await _productService.GetAllAsync();
            ApplyFiltersAndSort();
            StatusTextBlock.Text = $"Всего товаров: {_allProducts.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var context = ApplicationState.DbContext;
            var suppliers = await context.Suppliers.ToListAsync();

            var items = new List<Supplier>();
            items.Add(new Supplier { Id = 0, Name = "Все поставщики" });
            items.AddRange(suppliers);

            SupplierFilterComboBox.ItemsSource = items;
            SupplierFilterComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPickupPointsAsync()
    {
        try
        {
            var context = ApplicationState.DbContext;
            var pickupPoints = await context.PickupPoints.ToListAsync();
            PickupPointsDataGrid.ItemsSource = pickupPoints;
            StatusTextBlock.Text = $"Пунктов выдачи: {pickupPoints.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки пунктов выдачи: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFiltersAndSort()
    {
        _filteredProducts = new List<Product>(_allProducts);

        // Поиск
        var searchTerm = SearchTextBox.Text?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            _filteredProducts = _filteredProducts.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Description?.ToLower().Contains(searchTerm) ?? false) ||
                p.Article.ToLower().Contains(searchTerm) ||
                (p.Category?.Name?.ToLower().Contains(searchTerm) ?? false) ||
                (p.Manufacturer?.Name?.ToLower().Contains(searchTerm) ?? false) ||
                (p.Supplier?.Name?.ToLower().Contains(searchTerm) ?? false)
            ).ToList();
        }

        // Фильтр по поставщику
        if (SupplierFilterComboBox.SelectedItem is Supplier supplier && supplier.Id > 0)
        {
            _filteredProducts = _filteredProducts.Where(p => p.SupplierId == supplier.Id).ToList();
        }

        // Сортировка
        if (SortComboBox.SelectedItem is ComboBoxItem sortItem)
        {
            var sortText = sortItem.Content?.ToString();
            _filteredProducts = sortText switch
            {
                "Остаток (возр.)" => _filteredProducts.OrderBy(p => p.StockQuantity).ToList(),
                "Остаток (убыв.)" => _filteredProducts.OrderByDescending(p => p.StockQuantity).ToList(),
                _ => _filteredProducts
            };
        }

        ProductsListBox.ItemsSource = _filteredProducts;
        StatusTextBlock.Text = $"Показано товаров: {_filteredProducts.Count} из {_allProducts.Count}";
    }

    // Двойной клик - редактирование
    private void ProductsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProductsListBox.SelectedItem is Product product)
        {
            OpenProductWindow(product);
        }
    }

    // Поиск в реальном времени
    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFiltersAndSort();
    }

    // Сортировка
    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFiltersAndSort();
    }

    // Фильтр по поставщику
    private void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFiltersAndSort();
    }

    // Заказы
    private void OrdersButton_Click(object sender, RoutedEventArgs e)
    {
        if (_orderWindow == null || !_orderWindow.IsVisible)
        {
            _orderWindow = new OrderWindow();
            _orderWindow.Closed += (s, args) => _orderWindow = null;
            _orderWindow.Show();
        }
        else
        {
            _orderWindow.Activate();
        }
    }

    // Справочники
    private void DictionaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dictionaryWindow == null || !_dictionaryWindow.IsVisible)
        {
            _dictionaryWindow = new DictionaryWindow();
            _dictionaryWindow.Closed += (s, args) => { _dictionaryWindow = null; LoadProductsAsync(); };
            _dictionaryWindow.Show();
        }
        else
        {
            _dictionaryWindow.Activate();
        }
    }

    // Добавить товар
    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        OpenProductWindow(null);
    }

    // Редактировать товар
    private void EditProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsListBox.SelectedItem is Product product)
        {
            OpenProductWindow(product);
        }
        else
        {
            MessageBox.Show("Выберите товар для редактирования", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenProductWindow(Product? product)
    {
        if (_productWindow == null || !_productWindow.IsVisible)
        {
            _productWindow = new ProductWindow(product);
            _productWindow.Closed += (s, args) => { _productWindow = null; LoadProductsAsync(); };
            _productWindow.Show();
        }
        else
        {
            MessageBox.Show("Закройте окно редактирования товара перед открытием нового", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // Удалить товар
    private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsListBox.SelectedItem is Product product)
        {
            // Проверка: есть ли товар в заказах
            var context = ApplicationState.DbContext;
            var inOrder = await context.OrderItems.AnyAsync(oi => oi.ProductArticle == product.Article);

            if (inOrder)
            {
                MessageBox.Show("Невозможно удалить товар, так как он присутствует в заказе.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить товар '{product.Name}'?\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _productService.DeleteAsync(product.Article);
                    await LoadProductsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Выберите товар для удаления", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CloseProductWindow()
    {
        if (_productWindow != null && _productWindow.IsVisible)
        {
            _productWindow.Close();
        }
    }

    private void CloseDictionaryWindow()
    {
        if (_dictionaryWindow != null && _dictionaryWindow.IsVisible)
        {
            _dictionaryWindow.Close();
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        CloseProductWindow();
    }
}

public class async
{
}
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;

namespace WpfApp;

public partial class DictionaryWindow : Window
{
    private enum DictType { Categories, Manufacturers, Suppliers, PickupPoints }
    private DictType _currentType = DictType.Categories;

    private List<Category> _categories = new();
    private List<Manufacturer> _manufacturers = new();
    private List<Supplier> _suppliers = new();
    private List<PickupPoint> _pickupPoints = new();

    public DictionaryWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        Loaded += DictionaryWindow_Loaded;
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

    private async void DictionaryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CategoriesRadio.IsChecked = true;
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        var context = ApplicationState.DbContext;
        _categories = await context.Categories.ToListAsync();
        _manufacturers = await context.Manufacturers.ToListAsync();
        _suppliers = await context.Suppliers.ToListAsync();
        _pickupPoints = await context.PickupPoints.ToListAsync();
        RefreshGrid();
    }

    private void DictType_Changed(object sender, RoutedEventArgs e)
    {
        if (CategoriesRadio.IsChecked == true) _currentType = DictType.Categories;
        else if (ManufacturersRadio.IsChecked == true) _currentType = DictType.Manufacturers;
        else if (SuppliersRadio.IsChecked == true) _currentType = DictType.Suppliers;
        else if (PickupPointsRadio.IsChecked == true) _currentType = DictType.PickupPoints;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var idCol = new DataGridTextColumn { Header = "ID", Binding = new System.Windows.Data.Binding("Id"), Width = new DataGridLength(60) };

        switch (_currentType)
        {
            case DictType.Categories:
                DictDataGrid.Columns.Clear();
                DictDataGrid.Columns.Add(idCol);
                DictDataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                DictDataGrid.ItemsSource = _categories;
                break;

            case DictType.Manufacturers:
                DictDataGrid.Columns.Clear();
                DictDataGrid.Columns.Add(idCol);
                DictDataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                DictDataGrid.ItemsSource = _manufacturers;
                break;

            case DictType.Suppliers:
                DictDataGrid.Columns.Clear();
                DictDataGrid.Columns.Add(idCol);
                DictDataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new System.Windows.Data.Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                DictDataGrid.ItemsSource = _suppliers;
                break;

            case DictType.PickupPoints:
                DictDataGrid.Columns.Clear();
                DictDataGrid.Columns.Add(idCol);
                DictDataGrid.Columns.Add(new DataGridTextColumn { Header = "Адрес", Binding = new System.Windows.Data.Binding("Address"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                DictDataGrid.ItemsSource = _pickupPoints;
                break;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var title = _currentType switch
        {
            DictType.Categories => "Введите название категории",
            DictType.Manufacturers => "Введите название производителя",
            DictType.Suppliers => "Введите название поставщика",
            DictType.PickupPoints => "Введите адрес пункта выдачи",
            _ => "Введите название"
        };

        var inputWindow = new InputDialogWindow(title);
        inputWindow.Owner = this;
        if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputWindow.InputText))
        {
            AddItemAsync(inputWindow.InputText.Trim()).ConfigureAwait(false);
        }
    }

    private async Task AddItemAsync(string name)
    {
        var context = ApplicationState.DbContext;
        try
        {
            switch (_currentType)
            {
                case DictType.Categories:
                    context.Categories.Add(new Category { Name = name });
                    break;
                case DictType.Manufacturers:
                    context.Manufacturers.Add(new Manufacturer { Name = name });
                    break;
                case DictType.Suppliers:
                    context.Suppliers.Add(new Supplier { Name = name });
                    break;
                case DictType.PickupPoints:
                    context.PickupPoints.Add(new PickupPoint { Address = name });
                    break;
            }
            await context.SaveChangesAsync();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (DictDataGrid.SelectedItem == null)
        {
            MessageBox.Show("Выберите запись для редактирования", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currentValue = _currentType switch
        {
            DictType.Categories => (DictDataGrid.SelectedItem as Category)?.Name,
            DictType.Manufacturers => (DictDataGrid.SelectedItem as Manufacturer)?.Name,
            DictType.Suppliers => (DictDataGrid.SelectedItem as Supplier)?.Name,
            DictType.PickupPoints => (DictDataGrid.SelectedItem as PickupPoint)?.Address,
            _ => null
        };

        var title = _currentType switch
        {
            DictType.Categories => "Новое название категории",
            DictType.Manufacturers => "Новое название производителя",
            DictType.Suppliers => "Новое название поставщика",
            DictType.PickupPoints => "Новый адрес пункта выдачи",
            _ => "Введите новое название"
        };

        var inputWindow = new InputDialogWindow(title);
        inputWindow.Owner = this;
        inputWindow.InputText = currentValue ?? "";

        if (inputWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputWindow.InputText))
        {
            EditItemAsync(inputWindow.InputText.Trim()).ConfigureAwait(false);
        }
    }

    private async Task EditItemAsync(string newName)
    {
        var context = ApplicationState.DbContext;
        try
        {
            switch (_currentType)
            {
                case DictType.Categories:
                    if (DictDataGrid.SelectedItem is Category cat)
                    {
                        cat.Name = newName;
                        context.Categories.Update(cat);
                    }
                    break;
                case DictType.Manufacturers:
                    if (DictDataGrid.SelectedItem is Manufacturer man)
                    {
                        man.Name = newName;
                        context.Manufacturers.Update(man);
                    }
                    break;
                case DictType.Suppliers:
                    if (DictDataGrid.SelectedItem is Supplier sup)
                    {
                        sup.Name = newName;
                        context.Suppliers.Update(sup);
                    }
                    break;
                case DictType.PickupPoints:
                    if (DictDataGrid.SelectedItem is PickupPoint pp)
                    {
                        pp.Address = newName;
                        context.PickupPoints.Update(pp);
                    }
                    break;
            }
            await context.SaveChangesAsync();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DictDataGrid.SelectedItem == null)
        {
            MessageBox.Show("Выберите запись для удаления", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show("Удалить запись? Это действие нельзя отменить.\nВозможно запись используется в товарах.",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            DeleteItemAsync().ConfigureAwait(false);
        }
    }

    private async Task DeleteItemAsync()
    {
        var context = ApplicationState.DbContext;
        try
        {
            switch (_currentType)
            {
                case DictType.Categories:
                    if (DictDataGrid.SelectedItem is Category cat)
                        context.Categories.Remove(cat);
                    break;
                case DictType.Manufacturers:
                    if (DictDataGrid.SelectedItem is Manufacturer man)
                        context.Manufacturers.Remove(man);
                    break;
                case DictType.Suppliers:
                    if (DictDataGrid.SelectedItem is Supplier sup)
                        context.Suppliers.Remove(sup);
                    break;
                case DictType.PickupPoints:
                    if (DictDataGrid.SelectedItem is PickupPoint pp)
                        context.PickupPoints.Remove(pp);
                    break;
            }
            await context.SaveChangesAsync();
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}\nВозможно запись используется в товарах.",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using pgDataAccess.Models;
using pgDataAccess.Services;
using Microsoft.EntityFrameworkCore;

namespace WpfApp;

/// <summary>
/// Окно списка заказов. Полный доступ без авторизации.
/// </summary>
public partial class OrderWindow : Window
{
    private readonly OrderService _orderService;
    private List<Order> _allOrders = new();

    public OrderWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        _orderService = ApplicationState.OrderService;

        Loaded += OrderWindow_Loaded;
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

    private async void OrderWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
        Title = "Обувной магазин - Заказы";
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            _allOrders = await _orderService.GetAllAsync();
            OrdersListBox.ItemsSource = _allOrders;
            StatusTextBlock.Text = $"Всего заказов: {_allOrders.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OrdersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (OrdersListBox.SelectedItem is Order order)
        {
            var editWindow = new OrderEditWindow(order);
            editWindow.Owner = this;
            editWindow.ShowDialog();
            await LoadOrdersAsync();
        }
    }

    private async void AddOrderButton_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new OrderEditWindow();
        editWindow.Owner = this;
        editWindow.ShowDialog();
        await LoadOrdersAsync();
    }

    private async void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersListBox.SelectedItem is Order order)
        {
            var result = MessageBox.Show($"Удалить заказ №{order.Id}?\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _orderService.DeleteAsync(order.Id);
                    await LoadOrdersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления заказа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Выберите заказ для удаления", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

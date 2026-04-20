using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using pgDataAccess.Models;
using pgDataAccess.Services;

namespace WpfApp;

/// <summary>
/// Окно добавления/редактирования заказа.
/// По заданию: артикул (readonly), статус, пункт выдачи, даты.
/// Добавлять/редактировать может только администратор.
/// </summary>
public partial class OrderEditWindow : Window
{
    private readonly OrderService _orderService;
    private readonly Order? _editingOrder;
    private readonly bool _isNew;

    public OrderEditWindow(Order? order = null)
    {
        InitializeComponent();
        SetWindowIcon();
        _orderService = ApplicationState.OrderService;
        _editingOrder = order;
        _isNew = order == null;

        Loaded += OrderEditWindow_Loaded;
    }

    private async void OrderEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPickupPointsAsync();

        if (!_isNew && _editingOrder != null)
        {
            Title = "Редактирование заказа";
            ArticleTextBlock.Text = $"Заказ №{_editingOrder.Id}";

            // Устанавливаем статус
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == _editingOrder.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }

            PickupPointComboBox.SelectedValue = _editingOrder.PickupPointId;
            OrderDatePicker.SelectedDate = _editingOrder.OrderDate;
            DeliveryDatePicker.SelectedDate = _editingOrder.DeliveryDate;
        }
        else
        {
            Title = "Добавление заказа";
            ArticleTextBlock.Text = "Новый заказ";
            OrderDatePicker.SelectedDate = DateTime.Today;
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(7);
            StatusComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Загрузка пунктов выдачи в выпадающий список.
    /// </summary>
    private async Task LoadPickupPointsAsync()
    {
        var context = ApplicationState.DbContext;
        var pickupPoints = await context.PickupPoints.ToListAsync();
        PickupPointComboBox.ItemsSource = pickupPoints;
        PickupPointComboBox.SelectedValuePath = "Id";
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Сохранение заказа с валидацией полей.
    /// </summary>
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Валидация: пункт выдачи должен быть выбран
            if (PickupPointComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите пункт выдачи", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Валидация: статус должен быть выбран
            if (StatusComboBox.SelectedItem is not ComboBoxItem statusItem)
            {
                MessageBox.Show("Выберите статус заказа", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var status = statusItem.Content.ToString() ?? "Новый";

            if (_isNew)
            {
                // Для нового заказа нужен клиент — используем первого клиента
                var context = ApplicationState.DbContext;
                var client = await context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);

                if (client == null)
                {
                    MessageBox.Show("Не найден клиент для создания заказа.\nДобавьте клиента в справочник пользователей.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var order = new Order
                {
                    ClientId = client.Id,
                    PickupPointId = (int)PickupPointComboBox.SelectedValue,
                    OrderDate = DateTime.SpecifyKind(
    OrderDatePicker.SelectedDate ?? DateTime.Today,
    DateTimeKind.Utc),

                    DeliveryDate = DateTime.SpecifyKind(DeliveryDatePicker.SelectedDate ?? DateTime.Today.AddDays(7),DateTimeKind.Utc),
                    Status = status,
                    PickupCode = 0, // Генерируется автоматически в сервисе
                    OrderItems = new List<OrderItem>()
                };

                await _orderService.AddAsync(order);
                MessageBox.Show($"Заказ успешно создан!\nКод получения: {order.PickupCode}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (_editingOrder != null)
            {
                _editingOrder.PickupPointId = (int)PickupPointComboBox.SelectedValue;
                _editingOrder.OrderDate = OrderDatePicker.SelectedDate ?? DateTime.Today;
                _editingOrder.DeliveryDate = DeliveryDatePicker.SelectedDate ?? DateTime.Today.AddDays(7);
                _editingOrder.Status = status;

                await _orderService.UpdateAsync(_editingOrder);
                MessageBox.Show("Заказ успешно обновлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
}

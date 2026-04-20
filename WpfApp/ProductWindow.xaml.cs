using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using pgDataAccess.Models;
using pgDataAccess.Services;

namespace WpfApp;

public partial class ProductWindow : Window
{
    private readonly ProductService _productService;
    private readonly Product? _editingProduct;
    private readonly bool _isNew;
    private string? _oldPhotoPath;

    public ProductWindow(Product? product = null)
    {
        InitializeComponent();
        SetWindowIcon();
        _productService = ApplicationState.ProductService;
        _editingProduct = product;
        _isNew = product == null;
        _oldPhotoPath = product?.PhotoPath;

        Loaded += ProductWindow_Loaded;
    }

    private async void ProductWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadComboBoxesAsync();

        if (!_isNew && _editingProduct != null)
        {
            Title = "Редактирование товара";
            ArticleTextBox.Text = _editingProduct.Article;
            ArticleTextBox.IsEnabled = false;
            NameTextBox.Text = _editingProduct.Name;
            UnitTextBox.Text = _editingProduct.Unit;
            PriceTextBox.Text = _editingProduct.Price.ToString("F2");
            CategoryComboBox.SelectedValue = _editingProduct.CategoryId;
            ManufacturerComboBox.SelectedValue = _editingProduct.ManufacturerId;
            SupplierComboBox.SelectedValue = _editingProduct.SupplierId;
            DiscountTextBox.Text = _editingProduct.DiscountPercent.ToString();
            StockQuantityTextBox.Text = _editingProduct.StockQuantity.ToString();
            DescriptionTextBox.Text = _editingProduct.Description ?? "";
            PhotoPathTextBox.Text = _editingProduct.PhotoPath ?? "";
            LoadProductImage(_editingProduct.PhotoPath);
        }
        else
        {
            Title = "Добавление товара";
        }
    }

    private async Task LoadComboBoxesAsync()
    {
        var context = ApplicationState.DbContext;
        
        var categories = await context.Categories.ToListAsync();
        CategoryComboBox.ItemsSource = categories;
        CategoryComboBox.SelectedValuePath = "Id";

        var manufacturers = await context.Manufacturers.ToListAsync();
        ManufacturerComboBox.ItemsSource = manufacturers;
        ManufacturerComboBox.SelectedValuePath = "Id";

        var suppliers = await context.Suppliers.ToListAsync();
        SupplierComboBox.ItemsSource = suppliers;
        SupplierComboBox.SelectedValuePath = "Id";
    }

    private void LoadProductImage(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ProductImage.Source = bitmap;
            }
            catch
            {
                ProductImage.Source = null;
            }
        }
        else
        {
            ProductImage.Source = null;
        }
    }

    private void BrowsePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*",
            Title = "Выберите изображение товара"
        };

        if (dialog.ShowDialog() == true)
        {
            // Копируем изображение в папку приложения и изменяем размер
            try
            {
                var imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "product_images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                var fileName = Path.GetFileName(dialog.FileName);
                var destPath = Path.Combine(imagesFolder, fileName);

                // Изменяем размер до 300x200
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(dialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var resizedBitmap = new TransformedBitmap(bitmap, new ScaleTransform(
                    Math.Min(300.0 / bitmap.PixelWidth, 200.0 / bitmap.PixelHeight),
                    Math.Min(300.0 / bitmap.PixelWidth, 200.0 / bitmap.PixelHeight)));

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));

                using (var stream = new FileStream(destPath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                PhotoPathTextBox.Text = destPath;
                LoadProductImage(destPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeletePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        PhotoPathTextBox.Text = "";
        ProductImage.Source = null;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text))
            {
                MessageBox.Show("Введите артикул товара", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название товара", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Введите корректную цену (не отрицательное число)", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DiscountTextBox.Text, out int discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Введите корректный процент скидки (0-100)", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(StockQuantityTextBox.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Остаток не может быть отрицательным", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите категорию", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ManufacturerComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите производителя", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newPhotoPath = PhotoPathTextBox.Text.Trim();

            if (_isNew)
            {
                var product = new Product
                {
                    Article = ArticleTextBox.Text.Trim(),
                    Name = NameTextBox.Text.Trim(),
                    Unit = UnitTextBox.Text.Trim(),
                    Price = price,
                    CategoryId = (int)CategoryComboBox.SelectedValue,
                    ManufacturerId = (int)ManufacturerComboBox.SelectedValue,
                    SupplierId = (int)SupplierComboBox.SelectedValue,
                    DiscountPercent = discount,
                    StockQuantity = stock,
                    Description = DescriptionTextBox.Text.Trim(),
                    PhotoPath = newPhotoPath
                };

                await _productService.AddAsync(product);
                MessageBox.Show("Товар успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (_editingProduct != null)
            {
                // Удаляем старое фото если оно было и заменено
                if (!string.IsNullOrEmpty(_oldPhotoPath) && _oldPhotoPath != newPhotoPath && File.Exists(_oldPhotoPath))
                {
                    File.Delete(_oldPhotoPath);
                }

                _editingProduct.Name = NameTextBox.Text.Trim();
                _editingProduct.Unit = UnitTextBox.Text.Trim();
                _editingProduct.Price = price;
                _editingProduct.CategoryId = (int)CategoryComboBox.SelectedValue;
                _editingProduct.ManufacturerId = (int)ManufacturerComboBox.SelectedValue;
                _editingProduct.SupplierId = (int)SupplierComboBox.SelectedValue;
                _editingProduct.DiscountPercent = discount;
                _editingProduct.StockQuantity = stock;
                _editingProduct.Description = DescriptionTextBox.Text.Trim();
                _editingProduct.PhotoPath = newPhotoPath;

                await _productService.UpdateAsync(_editingProduct);
                MessageBox.Show("Товар успешно обновлен!", "Успех",
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

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Ничего не делаем, просто закрываем
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

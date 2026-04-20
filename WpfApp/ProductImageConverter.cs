using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace WpfApp;

/// <summary>
/// Конвертер для отображения изображений товаров.
/// Загружает картинки из папки images рядом с exe файлом.
/// </summary>
public class ProductImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var photoPath = value as string;
        Debug.WriteLine($"[ProductImageConverter] Value: '{photoPath ?? "null"}'");

        if (string.IsNullOrWhiteSpace(photoPath))
        {
            Debug.WriteLine($"[ProductImageConverter] Empty photo, returning placeholder");
            return CreatePlaceholderImage();
        }

        // Ищем картинку в папке images рядом с exe
        string? fullPath = FindImage(photoPath);

        if (fullPath != null && File.Exists(fullPath))
        {
            try
            {
                Debug.WriteLine($"[ProductImageConverter] Loading: {fullPath}");
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProductImageConverter] Error loading: {ex.Message}");
                return CreatePlaceholderImage();
            }
        }

        Debug.WriteLine($"[ProductImageConverter] Not found: {photoPath}");
        return CreatePlaceholderImage();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string? FindImage(string fileName)
    {
        // Вариант 1: папка images рядом с exe
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var path1 = Path.Combine(basePath, "images", fileName);
        if (File.Exists(path1)) return path1;

        // Вариант 2: исходная папка проекта (для отладки из VS)
        var projectPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "WpfApp", "images", fileName));
        if (File.Exists(projectPath)) return projectPath;

        // Вариант 3: текущая директория
        var path3 = Path.Combine(Directory.GetCurrentDirectory(), "images", fileName);
        if (File.Exists(path3)) return path3;

        return null;
    }

    private static BitmapImage CreatePlaceholderImage()
    {
        var placeholderPath = FindImage("picture.png");
        if (placeholderPath != null && File.Exists(placeholderPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(placeholderPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { }
        }

        // Если placeholder не найден, создаём 1x1 пустой
        return new BitmapImage();
    }
}

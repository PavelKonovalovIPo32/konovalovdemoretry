using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp;

/// <summary>
/// Конвертер для подсветки строки товара в зависимости от скидки и остатка.
/// Скидка >15% — зелёный фон #2E8B57.
/// Нет на складе (StockQuantity == 0) — голубой фон #ADD8E6.
/// </summary>
public class ProductRowBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is decimal discountPercent &&
            values[1] is int stockQuantity)
        {
            // Скидка >15% имеет приоритет
            if (discountPercent > 15m)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E8B57"));
            }

            // Нет на складе
            if (stockQuantity == 0)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ADD8E6"));
            }
        }

        return null!; // По умолчанию (белый из Setter)
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp;

/// <summary>
/// Конвертер для отображения имени связанного объекта с префиксом.
/// Параметр — это префикс (например "Категория: ").
/// Если объект null — возвращает "префикс—".
/// </summary>
public class RelatedNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var prefix = parameter as string ?? "";
        var fallback = prefix + "—";

        if (value == null)
            return fallback;

        // Пытаемся получить свойство Name
        var nameProperty = value.GetType().GetProperty("Name");
        if (nameProperty != null)
        {
            var name = nameProperty.GetValue(value) as string;
            if (!string.IsNullOrEmpty(name))
                return prefix + name;
        }

        return fallback;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

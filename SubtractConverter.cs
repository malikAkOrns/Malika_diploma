using System;
using System.Globalization;
using System.Windows.Data;

namespace MalikaDiploma.ViewModels.Converters
{
  internal sealed class SubtractConverter : IValueConverter
  {
    public double ToSubtract { get; }

    public SubtractConverter(double toSubtract)
    {
      ToSubtract = toSubtract;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double x)
      {
        return x - ToSubtract;
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new InvalidOperationException();
    }
  }
}
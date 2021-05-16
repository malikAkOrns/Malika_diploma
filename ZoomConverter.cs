using System;
using System.Windows;
using System.Windows.Data;

namespace MalikaDiploma.ViewModels.Converters
{
  public sealed class ZoomConverter : IValueConverter
  {
    private readonly double myZoomFactor;
    private readonly double myShift;

    public ZoomConverter(double zoomFactor, double shift)
    {
      if (zoomFactor == 0)
        throw new ArgumentOutOfRangeException(nameof(zoomFactor));

      myZoomFactor = zoomFactor;
      myShift = shift;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is double x)
      {
        return x * myZoomFactor + myShift;
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is double x)
      {
        return x / myZoomFactor - myShift;
      }

      return value;
    }
  }
}
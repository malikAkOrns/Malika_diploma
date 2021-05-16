using System;
using System.Windows.Data;

namespace MalikaDiploma.ViewModels.Converters
{
  public class CombiningConverter : IValueConverter
  {
    public IValueConverter Converter1 { get; }
    public IValueConverter Converter2 { get; }

    public CombiningConverter(IValueConverter converter1, IValueConverter converter2)
    {
      Converter1 = converter1;
      Converter2 = converter2;
    }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var convertedValue = Converter1.Convert(value, targetType, parameter, culture);
      return Converter2.Convert(convertedValue, targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
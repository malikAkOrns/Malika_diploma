using System;
using System.Windows;
using GalaSoft.MvvmLight;

namespace MalikaDiploma.ViewModels
{
  public class PathPointViewModel : ObservableObject
  {
    private double myY;
    private double myX;
    private string myName = "";

    public string Name
    {
      get => myName;
      set
      {
        if (string.IsNullOrWhiteSpace(value))
          throw new ArgumentException("Точка должна иметь имя");

        Set(ref myName, value);
      }
    }

    public double X
    {
      get => myX;
      set
      {
        Set(ref myX, value);
        RaisePropertyChanged(nameof(Point));
      }
    }

    public double Y
    {
      get => myY;
      set
      {
        Set(ref myY, value);
        RaisePropertyChanged(nameof(Point));
      }
    }

    public Point Point
    {
      get => new(X, Y);
      set
      {
        X = value.X;
        Y = value.Y;
      }
    }
  }
}
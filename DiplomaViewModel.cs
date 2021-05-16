using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MalikaDiploma.Models;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.ViewModels
{
  public class DiplomaViewModel : ObservableObject
  {
    private int myPointSelectedIndex;
    private SmoothingKind mySmoothingMode = SmoothingKind.C2Clothoid;
    private bool myShowGuides = true;
    private double mySmoothingFactor = 1.0;

    private double myZoomFactor = 200.0;
    private Point myShift = new (x: 150, y: 450);

    private double myHoverCurvature = double.NaN;
    private Point myHoverPoint;

    public DiplomaViewModel()
    {
      Points = new ObservableCollection<PathPointViewModel>();
      Points.CollectionChanged += PointsCollectionChanged;

      Points.Add(new PathPointViewModel {Name = "p0", X = 0.6, Y = -0.3});
      Points.Add(new PathPointViewModel {Name = "p1", X = -0.3, Y = 2.0});
      Points.Add(new PathPointViewModel {Name = "p2", X = 3.3, Y = 0.23});
      Points.Add(new PathPointViewModel {Name = "p3", X = 3.5, Y = 1.47});

      PointCoordsChanged();
    }

    public string Title
    {
      get
      {
        var builder = new StringBuilder("Дипломная работа. Тишабаева М. Р.");

        if (HoverPoint.X is not double.NaN)
          builder.Append($" (x = {HoverPoint.X:N3} y = {HoverPoint.Y:N3})");

        if (HoverCurvature is not double.NaN)
          builder.Append($" (curvature = {HoverCurvature})");

        return builder.ToString();
      }
    }

    public Point HoverPoint
    {
      get => myHoverPoint;
      set
      {
        Set(ref myHoverPoint, value);
        RaisePropertyChanged(nameof(Title));
      }
    }

    public double HoverCurvature
    {
      get => myHoverCurvature;
      set
      {
        Set(ref myHoverCurvature, value);
        RaisePropertyChanged(nameof(Title));
      }
    }

    public ObservableCollection<PathPointViewModel> Points { get; }

    public ObservableCollection<Point> Dots { get; } = new();

    public event EventHandler? PathChanged;

    public double ZoomFactor
    {
      get => myZoomFactor;
      set
      {
        Set(ref myZoomFactor, value);
        PathChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public Point Shift
    {
      get => myShift;
      set
      {
        Set(ref myShift, value);
        PathChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public enum SmoothingKind
    {
      C1Arc,
      C2Cubic,
      C2Clothoid
    }

    public SmoothingKind SmoothingMode
    {
      get => mySmoothingMode;
      set
      {
        if (Set(ref mySmoothingMode, value))
        {
          base.RaisePropertyChanged(nameof(IsC1ArcEnabled));
          base.RaisePropertyChanged(nameof(IsC2CubicEnabled));
          base.RaisePropertyChanged(nameof(IsC2ClothoidEnabled));
        }
      }
    }

    public bool IsC1ArcEnabled => SmoothingMode == SmoothingKind.C1Arc;
    public bool IsC2CubicEnabled => SmoothingMode == SmoothingKind.C2Cubic;
    public bool IsC2ClothoidEnabled => SmoothingMode == SmoothingKind.C2Clothoid;

    public RelayCommand EnableC1Arc
    {
      get
      {
        return new(
          execute: () =>
          {
            SmoothingMode = SmoothingKind.C1Arc;
            PathChanged?.Invoke(this, EventArgs.Empty);
          },
          canExecute: () => true);
      }
    }

    public RelayCommand EnableC2Cubic
    {
      get
      {
        return new(
          execute: () =>
          {
            SmoothingMode = SmoothingKind.C2Cubic;
            PathChanged?.Invoke(this, EventArgs.Empty);
          },
          canExecute: () => true);
      }
    }

    public RelayCommand EnableC2Clothoid
    {
      get
      {
        return new(
          execute: () =>
          {
            SmoothingMode = SmoothingKind.C2Clothoid;
            PathChanged?.Invoke(this, EventArgs.Empty);
          },
          canExecute: () => true);
      }
    }

    public bool ShowGuides
    {
      get => myShowGuides;
      set
      {
        Set(ref myShowGuides, value);
        PathChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    private void PointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
      if (args.Action != NotifyCollectionChangedAction.Add) return;

      if (args.NewItems == null) return;

      var newIndex = Points.Count - 1;

      foreach (PathPointViewModel newItem in args.NewItems)
      {
        newItem.PropertyChanged += (_, _) => PointCoordsChanged();

        if (string.IsNullOrWhiteSpace(newItem.Name))
        {
          newItem.Name = "p" + newIndex;
          newIndex++;
        }
      }
    }

    private void PointCoordsChanged()
    {
      PathChanged?.Invoke(this, EventArgs.Empty);
    }

    public int SelectedPointIndex
    {
      get => myPointSelectedIndex;
      set => Set(ref myPointSelectedIndex, value);
    }

    public double SmoothingFactor
    {
      get => mySmoothingFactor;
      set
      {
        if (value < 0 || value > 1.0)
          throw new ArgumentOutOfRangeException();

        Set(ref mySmoothingFactor, value);
        PathChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public IEnumerable<PathPart> GetSmoothedPath()
    {
      var clothoidPart = ClothoidPart.FromCurvature(new Point(0.5, 0.5), curvature: Math.PI / 3);

      clothoidPart = clothoidPart.Flip().Reverse();

      //return new PathPart[] {clothoidPart};

      return Points
        .Select(viewModel => viewModel.Point)
        .Triplets()
        .Select(t =>
        {
          return SmoothingMode switch
          {
            SmoothingKind.C1Arc => Algorithms.SmoothC1Arc(t.first, t.second, t.third, SmoothingFactor),
            SmoothingKind.C2Cubic => Algorithms.SmoothC2Cubic(t.first, t.second, t.third, SmoothingFactor),
            SmoothingKind.C2Clothoid => Algorithms.SmoothC2Clothoid(t.first, t.second, t.third, SmoothingFactor),
            _ => throw new ArgumentOutOfRangeException()
          };
        })
        .JoinIntersectingSmoothings();
    }

    public CurvaturePlot? CurvaturePlot { get; set; }

    public RelayCommand MoveUpCommand
    {
      get
      {
        return new(
          execute: () =>
          {
            var index = SelectedPointIndex;
            var point = Points[index];
            Points.RemoveAt(index);
            Points.Insert(index - 1, point);
            SelectedPointIndex = index - 1;
          },
          canExecute: () => SelectedPointIndex > 0);
      }
    }

    public RelayCommand MoveDownCommand
    {
      get
      {
        return new(
          execute: () =>
          {
            var index = SelectedPointIndex;
            var point = Points[index];
            Points.RemoveAt(index);
            Points.Insert(index + 1, point);
            SelectedPointIndex = index + 1;
          },
          canExecute: () => SelectedPointIndex >= 0 && SelectedPointIndex + 1 < Points.Count);
      }
    }

    public RelayCommand ClearCommand
    {
      get
      {
        return new(
          execute: () =>
          {
            Points.Clear();
            SelectedPointIndex = -1;
          },
          canExecute: () => Points.Count > 0);
      }
    }
  }
}
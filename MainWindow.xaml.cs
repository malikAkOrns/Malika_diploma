using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MalikaDiploma.Models;
using MalikaDiploma.Models.Parts;
using MalikaDiploma.ViewModels;
using MalikaDiploma.ViewModels.Converters;

namespace MalikaDiploma.Views
{
  public partial class MainWindow
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    public DiplomaViewModel Diploma { get; } = new();

    private void Canvas_OnLoaded(object sender, RoutedEventArgs e)
    {
      Update();
      DrawSmoothedPath();
      DrawCurvatureGraph();

      Diploma.Points.CollectionChanged += (_, _) => Update();
      Diploma.Dots.CollectionChanged += (_, _) => Update();
      Diploma.PathChanged += (_, _) =>
      {
        DrawSmoothedPath();
        DrawCurvatureGraph();
      };
    }

    #region Mouse handling

    private Point GetCoords(MouseEventArgs args)
    {
      return args.GetPosition(Plane)
        .Shift(Diploma.Shift.InvertSigns())
        .Scale(1 / Diploma.ZoomFactor, -1 / Diploma.ZoomFactor);
    }

    private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
    {
      var coords = GetCoords(e);

      Diploma.HoverPoint = coords;

      if (e.LeftButton == MouseButtonState.Pressed)
      {
        var selectedIndex = Diploma.SelectedPointIndex;
        if (selectedIndex >= 0)
        {
          var pointModel = Diploma.Points[selectedIndex];

          pointModel.X = coords.X;
          pointModel.Y = coords.Y;
        }
      }
    }

    private void PathPointBorderMouseDown(object sender, MouseEventArgs e)
    {
      var border = (Border) sender;
      var pathPointModel = (PathPointViewModel) border.DataContext;

      if (e.LeftButton == MouseButtonState.Pressed)
      {
        Diploma.SelectedPointIndex = Diploma.Points.IndexOf(pathPointModel);
        e.Handled = true;
      }
      else if (e.RightButton == MouseButtonState.Pressed)
      {
        Diploma.Points.Remove(pathPointModel);
      }
    }

    private void Canvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2)
      {
        var position = GetCoords(e);
        Diploma.Points.Add(new PathPointViewModel {X = position.X, Y = position.Y});
      }
      else if (e.ClickCount == 1)
      {
        Diploma.SelectedPointIndex = -1;
      }
    }

    #endregion
    #region Drawing

    private void Update()
    {
      Plane.Children.Clear();

      DrawOrigin();

      PathPointViewModel? previous = null;

      foreach (var pathPoint in Diploma.Points)
      {
        DrawPathPointDotAndLabel(pathPoint);

        if (previous != null)
          DrowPathPointLine(previous, pathPoint);

        previous = pathPoint;
      }

      foreach (var diplomaDot in Diploma.Dots)
      {
        var pathPointDot = new Ellipse
        {
          Width = 6,
          Height = 6,
          Fill = Brushes.Red,
          IsHitTestVisible = false
        };

        pathPointDot.SetValue(Canvas.LeftProperty, diplomaDot.X * Diploma.ZoomFactor - (pathPointDot.Width / 2) + Diploma.Shift.X);
        pathPointDot.SetValue(Canvas.TopProperty, diplomaDot.Y * -Diploma.ZoomFactor - (pathPointDot.Height / 2) + Diploma.Shift.Y);

        pathPointDot.SetValue(Panel.ZIndexProperty, 100);

        Plane.Children.Add(pathPointDot);
      }

      PrepareSmoothedPath();
      DrawSmoothedPath();
    }

    private void PrepareSmoothedPath()
    {
      var smoothedPath = new Path
      {
        Name = "smoothedPath",
        StrokeThickness = 4,
        Stroke = new SolidColorBrush(Colors.Blue),
        IsHitTestVisible = false
      };

      Plane.Children.Add(smoothedPath);

      var circlesPath = new Path
      {
        Name = "circlesPath",
        //StrokeDashArray = {4, 2},
        StrokeThickness = 1.5,
        Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
        IsHitTestVisible = false
      };

      circlesPath.SetValue(Panel.ZIndexProperty, -20);

      Plane.Children.Add(circlesPath);

      var extendedPath1 = new Path
      {
        Name = "extendedPath1",
        StrokeDashArray = {8, 3},
        StrokeThickness = 2,
        Stroke = new SolidColorBrush(Color.FromRgb(60, 205, 60)),
        IsHitTestVisible = false
      };

      extendedPath1.SetValue(Panel.ZIndexProperty, 0);

      Plane.Children.Add(extendedPath1);

      var extendedPath2 = new Path
      {
        Name = "extendedPath2",
        StrokeDashArray = {8, 2},
        StrokeThickness = 2,
        Stroke = new SolidColorBrush(Color.FromRgb(230, 140, 140)),
        IsHitTestVisible = false
      };

      extendedPath2.SetValue(Panel.ZIndexProperty, 0);

      Plane.Children.Add(extendedPath2);

      var jointsPath = new Path
      {
        Name = "joints",
        StrokeThickness = 1.5,
        Fill = new SolidColorBrush(Colors.White),
        Stroke = new SolidColorBrush(Colors.Blue),
        IsHitTestVisible = false
      };

      jointsPath.SetValue(Panel.ZIndexProperty, 1);

      Plane.Children.Add(jointsPath);
    }

    private void DrawSmoothedPath()
    {
      Path GetPlanePath(string name)
      {
        return Plane.Children.OfType<Path>().First(x => x.Name == name);
      }

      var smoothedPath = GetPlanePath("smoothedPath");
      var extendedPath1 = GetPlanePath("extendedPath1");
      var extendedPath2 = GetPlanePath("extendedPath2");
      var circlesPath = GetPlanePath("circlesPath");
      var jointsPath = GetPlanePath("joints");

      var pathFigure = new PathFigure();
      var extendedPathFigure1 = new PathFigure();
      var extendedPathFigure2 = new PathFigure();
      var circlesGroup = new GeometryGroup();
      var jointsGroup = new GeometryGroup();
      var firstPart = true;

      var zoomFactor = Diploma.ZoomFactor;
      var shift = Diploma.Shift;

      Point Transform(Point point) =>
        new Point(point.X * zoomFactor, point.Y * -zoomFactor).Shift(shift);

      foreach (var part in Diploma.GetSmoothedPath())
      {
        if (firstPart)
          pathFigure.StartPoint = Transform(part.From);
        else
          pathFigure.Segments.Add(new LineSegment(Transform(part.From), isStroked: true));

        switch (part)
        {
          case LinePart line:
          {
            pathFigure.Segments.Add(new LineSegment(Transform(line.From), isStroked: true));
            pathFigure.Segments.Add(new LineSegment(Transform(line.To), isStroked: true));
            break;
          }

          case ArcPart arc:
          {
            var arcRadius = arc.Radius * zoomFactor;

            pathFigure.Segments.Add(new ArcSegment(
              point: Transform(arc.To),
              size: new Size(arcRadius, arcRadius),
              rotationAngle: 0,
              isLargeArc: false, // never >= 180 degrees
              sweepDirection: arc.Direction ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
              isStroked: true));

            if (arcRadius < 1000 && Diploma.ShowGuides)
            {
              circlesGroup.Children.Add(new EllipseGeometry(
                center: Transform(arc.Center),
                radiusX: arcRadius,
                radiusY: arcRadius));
            }

            break;
          }

          case CubicPart cubic:
          {
            var delta = cubic.Width / 100;

            foreach (var point in cubic.Tabulate(delta))
            {
              pathFigure.Segments.Add(new LineSegment(Transform(point), isStroked: true));
            }

            if (Diploma.ShowGuides)
            {
              var path = cubic.IsReversed ? extendedPathFigure2 : extendedPathFigure1;
              var first = true;

              foreach (var point in cubic.Extend().Tabulate(delta))
              {
                path.Segments.Add(new LineSegment(Transform(point), isStroked: !first));
                first = false;
              }

              first = true;

              foreach (var point in cubic.Rotate180().Extend().Tabulate(delta))
              {
                path.Segments.Add(new LineSegment(Transform(point), isStroked: !first));
                first = false;
              }

              jointsGroup.Children.Add(new EllipseGeometry(center: Transform(cubic.From), radiusX: 4, radiusY: 4));
              jointsGroup.Children.Add(new EllipseGeometry(center: Transform(cubic.To), radiusX: 4, radiusY: 4));
            }

            break;
          }

          case ClothoidPart clothoidPart:
          {
            var delta = clothoidPart.Length / 100;

            foreach (var point in clothoidPart.Tabulate(delta))
            {
              pathFigure.Segments.Add(new LineSegment(Transform(point), isStroked: true));
            }

            if (Diploma.ShowGuides)
            {
              var path = clothoidPart.IsReversed ? extendedPathFigure2 : extendedPathFigure1;
              var first = true;

              foreach (var point in clothoidPart.Expand().Tabulate(delta))
              {
                path.Segments.Add(new LineSegment(Transform(point), isStroked: !first));
                first = false;
              }

              jointsGroup.Children.Add(new EllipseGeometry(center: Transform(clothoidPart.From), radiusX: 4, radiusY: 4));
              jointsGroup.Children.Add(new EllipseGeometry(center: Transform(clothoidPart.To), radiusX: 4, radiusY: 4));
            }

            break;
          }
        }

        firstPart = false;
      }

      smoothedPath.Data = new PathGeometry { Figures = { pathFigure } };
      extendedPath1.Data = new PathGeometry { Figures = { extendedPathFigure1 } };
      extendedPath2.Data = new PathGeometry { Figures = { extendedPathFigure2 } };
      circlesPath.Data = circlesGroup;
      jointsPath.Data = jointsGroup;
    }

    private void DrawOrigin()
    {
      var shift = Diploma.Shift;

      var length = Math.Max(Plane.ActualWidth, Plane.ActualHeight) * 2;

      var yLine = new Line
      {
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
        X1 = -length + shift.X, Y1 = 0 + shift.Y,
        X2 = +length + shift.X, Y2 = 0 + shift.Y
      };

      yLine.SetValue(Panel.ZIndexProperty, -100);

      var xLine = new Line
      {
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
        X1 = 0 + shift.X, Y1 = -length + shift.Y,
        X2 = 0 + shift.X, Y2 = +length + shift.Y
      };

      xLine.SetValue(Panel.ZIndexProperty, -100);

      Plane.Children.Add(yLine);
      Plane.Children.Add(xLine);

      var yLabel = new TextBlock { Text = "Y", FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
      yLabel.SetValue(Canvas.LeftProperty, shift.X + 5);
      yLabel.SetValue(Canvas.TopProperty, 5.0);
      yLabel.SetValue(Panel.ZIndexProperty, -100);

      Plane.Children.Add(yLabel);

      var xLabel = new TextBlock { Text = "X", FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
      xLabel.SetValue(Canvas.LeftProperty, Plane.ActualWidth - xLabel.ActualWidth - 15);
      xLabel.SetValue(Canvas.TopProperty, shift.Y + 5);
      xLabel.SetValue(Panel.ZIndexProperty, -100);

      Plane.Children.Add(xLabel);

      var minX = Round(-shift.X / Diploma.ZoomFactor);
      var maxX = Round((Plane.ActualWidth - shift.X) / Diploma.ZoomFactor);
      var minY = -Round((Plane.ActualHeight - shift.Y) / Diploma.ZoomFactor);
      var maxY = -Round(-shift.Y / Diploma.ZoomFactor);

      for (var index = minX; index < maxX; index++)
      {
        var xDigit = new TextBlock { Text = index.ToString(), Foreground = Brushes.Gray };
        xDigit.SetValue(Canvas.LeftProperty, shift.X + index * Diploma.ZoomFactor + (index == 0 ? +5 : -3));
        xDigit.SetValue(Canvas.TopProperty, shift.Y + 5);
        xDigit.SetValue(Panel.ZIndexProperty, -100);

        Plane.Children.Add(xDigit);
      }

      var ratio = 20;
      for (var index = minX * ratio; index < maxX * ratio; index++)
      {
        var tickLength = index % ratio == 0 ? 4 : 2;
        var x = shift.X + index * Diploma.ZoomFactor / ratio;
        var xTickLine = new Line
        {
          StrokeThickness = 1,
          Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
          X1 = x, Y1 = shift.Y - tickLength,
          X2 = x, Y2 = shift.Y + tickLength
        };

        xTickLine.SetValue(Panel.ZIndexProperty, -90);

        Plane.Children.Add(xTickLine);
      }

      for (var index = minY; index < maxY; index++)
      {
        if (index == 0) continue;

        var yDigit = new TextBlock { Text = index.ToString(), Foreground = Brushes.Gray };
        yDigit.SetValue(Canvas.LeftProperty, shift.X + 8);
        yDigit.SetValue(Canvas.TopProperty, shift.Y + index * -Diploma.ZoomFactor - 8);
        yDigit.SetValue(Panel.ZIndexProperty, -100);

        Plane.Children.Add(yDigit);
      }

      for (var index = minY * ratio; index < maxY * ratio; index++)
      {
        if (index == 0) continue;

        var tickLength = index % ratio == 0 ? 4 : 2;
        var y = shift.Y + index * -Diploma.ZoomFactor / ratio;
        var yTickLine = new Line
        {
          StrokeThickness = 1,
          Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
          X1 = shift.X - tickLength, Y1 = y,
          X2 = shift.X + tickLength, Y2 = y
        };

        yTickLine.SetValue(Panel.ZIndexProperty, -90);

        Plane.Children.Add(yTickLine);
      }

      static int Round(double x) => (int) (x > 0 ? Math.Ceiling(x) : Math.Floor(x));
    }

    private void DrawPathPointDotAndLabel(PathPointViewModel pathPointView)
    {
      var borderText = new TextBlock
      {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 14,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.DarkGray
      };

      borderText.SetBinding(TextBlock.TextProperty, nameof(PathPointViewModel.Name));

      var border = new Border
      {
        Background = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
        BorderBrush = Brushes.DarkGray,
        Height = 26,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(1),
        Padding = new Thickness(10, 0, 10, 0),
        DataContext = pathPointView,
        Child = borderText
      };

      border.SetBinding(Canvas.LeftProperty, new Binding("X") { Mode = BindingMode.OneWay, Converter = new ZoomConverter(Diploma.ZoomFactor, Diploma.Shift.X) });
      border.SetBinding(Canvas.TopProperty, new Binding("Y") { Mode = BindingMode.OneWay, Converter = new ZoomConverter(-Diploma.ZoomFactor, Diploma.Shift.Y) });
      border.SetValue(Panel.ZIndexProperty, 0);
      border.MouseDown += PathPointBorderMouseDown;

      Plane.Children.Add(border);

      var pathPointDot = new Ellipse
      {
        Width = 6,
        Height = 6,
        Fill = Brushes.Black,
        DataContext = pathPointView,
        IsHitTestVisible = false
      };

      pathPointDot.SetBinding(Canvas.LeftProperty, new Binding("X")
      {
        Mode = BindingMode.OneWay,
        Converter = new CombiningConverter(new ZoomConverter(Diploma.ZoomFactor, Diploma.Shift.X), new SubtractConverter(3))
      });

      pathPointDot.SetBinding(Canvas.TopProperty, new Binding("Y")
      {
        Mode = BindingMode.OneWay,
        Converter = new CombiningConverter(new ZoomConverter(-Diploma.ZoomFactor, Diploma.Shift.Y), new SubtractConverter(3))
      });

      pathPointDot.SetValue(Panel.ZIndexProperty, 100);

      Plane.Children.Add(pathPointDot);
    }

    private void DrowPathPointLine(PathPointViewModel previous, PathPointViewModel pathPoint)
    {
      var line = new Line
      {
        StrokeThickness = 2,
        StrokeDashArray = {6, 2},
        Stroke = new SolidColorBrush(Color.FromArgb(200, 210, 210, 210)),
        DataContext = new LineViewModel {From = previous, To = pathPoint},
        IsHitTestVisible = false
      };

      line.SetBinding(Line.X1Property, new Binding("From.X") {Mode = BindingMode.OneWay, Converter = new ZoomConverter(Diploma.ZoomFactor, Diploma.Shift.X) });
      line.SetBinding(Line.Y1Property, new Binding("From.Y") {Mode = BindingMode.OneWay, Converter = new ZoomConverter(-Diploma.ZoomFactor, Diploma.Shift.Y) });
      line.SetBinding(Line.X2Property, new Binding("To.X") {Mode = BindingMode.OneWay, Converter = new ZoomConverter(Diploma.ZoomFactor, Diploma.Shift.X) });
      line.SetBinding(Line.Y2Property, new Binding("To.Y") {Mode = BindingMode.OneWay, Converter = new ZoomConverter(-Diploma.ZoomFactor, Diploma.Shift.Y) });
      line.SetValue(Panel.ZIndexProperty, -20);

      Plane.Children.Add(line);
    }

    #endregion
    #region Curvature graph

    private void DrawCurvatureGraph()
    {
      CurvatureGraph.Children.Clear();

      var plotPath = new Path
      {
        StrokeThickness = 2,
        Stroke = new SolidColorBrush(Colors.Blue),
        IsHitTestVisible = false
      };

      var smoothedPath = Diploma.GetSmoothedPath().ToList();
      var curvaturePlot = CurvaturePlot.Create(smoothedPath, pointsCount: 1 * (int) CurvatureGraph.ActualWidth);

      Diploma.CurvaturePlot = curvaturePlot;

      var minX = 5;
      var scaledPlot = curvaturePlot.Scale(
        minY: 25, maxY: CurvatureGraph.ActualHeight - 5,
        minX: minX, maxX: CurvatureGraph.ActualWidth - 10);

      var pathFigure = new PathFigure();

      var first = true;
      foreach (var point in scaledPlot.Points)
      {
        if (first)
        {
          pathFigure.StartPoint = point;
          first = false;
        }
        else
        {
          pathFigure.Segments.Add(new LineSegment(point, isStroked: true));
        }
      }

      plotPath.Data = new PathGeometry { Figures = { pathFigure } };
      CurvatureGraph.Children.Add(plotPath);

      var yLine = new Line
      {
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
        X1 = minX, Y1 = 0,
        X2 = minX, Y2 = CurvatureGraph.ActualHeight
      };

      yLine.SetValue(Panel.ZIndexProperty, -100);
      CurvatureGraph.Children.Add(yLine);

      var xLine = new Line
      {
        StrokeThickness = 1,
        Stroke = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
        X1 = 0, Y1 = scaledPlot.ZeroY,
        X2 = CurvatureGraph.ActualWidth, Y2 = scaledPlot.ZeroY
      };

      xLine.SetValue(Panel.ZIndexProperty, -100);
      CurvatureGraph.Children.Add(xLine);

      var xLabel = new TextBlock { Text = "L", FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
      xLabel.SetValue(Canvas.LeftProperty, CurvatureGraph.ActualWidth - xLabel.ActualWidth - 15);
      xLabel.SetValue(Canvas.TopProperty, scaledPlot.ZeroY + 5);
      xLabel.SetValue(Panel.ZIndexProperty, -100);

      CurvatureGraph.Children.Add(xLabel);

      var yMinLabel = new TextBlock { Text = $"1/R = {curvaturePlot.MinY:N2}", FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray };
      yMinLabel.SetValue(Canvas.LeftProperty, minX + 5.0);
      yMinLabel.SetValue(Canvas.TopProperty, +5.0);
      yMinLabel.SetValue(Panel.ZIndexProperty, -100);

      CurvatureGraph.Children.Add(yMinLabel);

      var yMaxLabel = new TextBlock { Text = $"1/R = {curvaturePlot.MaxY:N2}", FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray };
      yMaxLabel.SetValue(Canvas.LeftProperty, minX + 5.0);
      yMaxLabel.SetValue(Canvas.TopProperty, CurvatureGraph.ActualHeight - 25);
      yMaxLabel.SetValue(Panel.ZIndexProperty, -100);

      CurvatureGraph.Children.Add(yMaxLabel);

      var maxX = (int) Math.Ceiling(curvaturePlot.MaxX);
      var step = scaledPlot.MaxX / curvaturePlot.MaxX;

      for (var index = 0; index < maxX; index++)
      {
        var xDigit = new TextBlock { Text = index.ToString(), Foreground = Brushes.Gray };
        xDigit.SetValue(Canvas.LeftProperty, minX + index * step + (index == 0 ? +5 : -3));
        xDigit.SetValue(Canvas.TopProperty, scaledPlot.ZeroY + 3);
        xDigit.SetValue(Panel.ZIndexProperty, -100);

        CurvatureGraph.Children.Add(xDigit);
      }

      var ratio = 10;
      for (var index = 0; index < maxX * ratio; index++)
      {
        var tickLength = index % ratio == 0 ? 4 : 2;
        var x = minX + index * step / ratio;
        var xTickLine = new Line
        {
          StrokeThickness = 1,
          Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
          X1 = x, Y1 = scaledPlot.ZeroY - tickLength,
          X2 = x, Y2 = scaledPlot.ZeroY + tickLength
        };

        xTickLine.SetValue(Panel.ZIndexProperty, -90);

        CurvatureGraph.Children.Add(xTickLine);
      }
    }

    private void CurvatureGraphMouseLeave(object sender, MouseEventArgs e)
    {
      Diploma.Dots.Clear();
      Diploma.HoverCurvature = double.NaN;
    }

    private void CurvatureGraphMouseMove(object sender, MouseEventArgs e)
    {
      var positionInGraph = e.GetPosition(CurvatureGraph);

      var ratio = positionInGraph.X / CurvatureGraph.ActualWidth;

      if (Diploma.Dots.Count == 0)
      {
        Diploma.Dots.Add(new Point());
      }

      var pathParts = Diploma.GetSmoothedPath().ToList();
      var length = pathParts.Sum(x => x.Length);

      var point = PathPart.FindPositionInPath(pathParts, length * ratio);
      if (point != null)
      {
        Diploma.Dots[0] = point.Value;
      }

      var plot = Diploma.CurvaturePlot;
      if (plot != null)
      {
        var index = (int)(plot.Points.Count * ratio);
        if (index < plot.Points.Count)
        {
          Diploma.HoverCurvature = plot.Points[index].Y;
        }
      }
    }

    #endregion

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      Update();
      DrawCurvatureGraph();
    }

    private void Plane_OnKeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Up:
          Diploma.Shift = Diploma.Shift.Move(dx: 0, dy: -100);
          break;
        case Key.Down:
          Diploma.Shift = Diploma.Shift.Move(dx: 0, dy: +100);
          break;
        case Key.Left:
          Diploma.Shift = Diploma.Shift.Move(dx: +100, dy: 0);
          break;
        case Key.Right:
          Diploma.Shift = Diploma.Shift.Move(dx: -100, dy: 0);
          break;
        case Key.OemPlus:
          Diploma.ZoomFactor *= 1.1;
          break;
        case Key.OemMinus:
          Diploma.ZoomFactor /= 1.1;
          break;
      }

      Update();
    }
  }
}
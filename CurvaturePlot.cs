using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Построитель графика кривизны участков пути
  public class CurvaturePlot
  {
    public double MinX { get; private init; }
    public double MaxX { get; private init; }
    public double MinY { get; private init; }
    public double MaxY { get; private init; }
    public double ZeroY { get; private init; }

    public IReadOnlyList<Point> Points { get; private init; } = null!;

    public static CurvaturePlot Create(IReadOnlyList<PathPart> smoothedPath, int pointsCount)
    {
      var totalLength = PathPart.TotalLength(smoothedPath);
      var delta = totalLength / pointsCount;

      const int measurements = 10;
      var tabulationPoints = PathPart
        .Tabulate(smoothedPath, delta: delta / measurements)
        .TripletsWithDistance(distanceBetween: 0)
        .Where(x => x.first.part == x.second.part && x.second.part == x.third.part)
        .Select(x => (x.first.point, x.second.point, x.third.point));

      var points = new List<Point>();

      var position = 0.0;
      var maxY = 0.0;
      var minY = 0.0;

      var measures = new List<double>();

      // взятие каждых трех подряд идущих точек пути
      foreach (var (a, b, c) in tabulationPoints)
      {
        var curvature = Helpers.TriPointCurvature(a, b, c) / -2.0;
        if (curvature is double.NaN)
          curvature = 0.0;

        measures.Add(curvature);

        if (measures.Count >= measurements)
        {
          var average = measures.Average();
          measures.Clear();

          maxY = Math.Max(maxY, average);
          minY = Math.Min(minY, average);

          points.Add(new Point(x: position, y: average));
          position += delta;
        }
      }

      return new CurvaturePlot
      {
        MinX = 0,
        MaxX = totalLength,
        MinY = minY,
        MaxY = maxY,
        ZeroY = 0,
        Points = points
      };
    }

    // Масштабирование графика кривизны
    public CurvaturePlot Scale(double minY, double maxY, double minX, double maxX)
    {
      if (minY >= maxY) throw new ArgumentOutOfRangeException();

      var oldHeight = MaxY - MinY;
      var newHeight = maxY - minY;

      var oldWidth = MaxX - MinX;
      var newWidth = maxX - minX;

      var newPoints = new List<Point>(Points);

      double ScaleY(double y) => (y - MinY) / oldHeight * newHeight + minY;
      double ScaleX(double x) => (x - MinX) / oldWidth * newWidth + minX;

      for (var index = 0; index < newPoints.Count; index++)
      {
        var point = newPoints[index];

        point.X = ScaleX(point.X);
        point.Y = ScaleY(point.Y);

        newPoints[index] = point;
      }

      return new CurvaturePlot
      {
        MinY = minY,
        MaxY = maxY,
        MinX = minX,
        MaxX = maxX,
        ZeroY = oldHeight == 0 ? 0 : ScaleY(ZeroY),
        Points = newPoints
      };
    }
  }
}
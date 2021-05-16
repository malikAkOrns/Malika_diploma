using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Windows;

namespace MalikaDiploma.Models.Parts
{
  // Часть пути - Кубическая парабола
  // Задается координатой длиной проекции на ось X, коэффициентом K и направлением,
  // а на плоскости раполагается точкой начала и углом поворота относительно центра координат
  public sealed record CubicPart : PathPart
  {
    // Создание экземпляра части пути
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public CubicPart(Point From, double Width, double Angle, bool IsReversed = false, double K = 2.0)
    {
      if (Width < 0)
        throw new ArgumentOutOfRangeException();
      if (K == 0)
        throw new ArgumentOutOfRangeException();

      this.From = From;
      this.Width = Width;
      this.Angle = Angle;
      this.IsReversed = IsReversed;
      this.K = K;
    }

    // Координата начальной точки параболы
    public override Point From { get; init; }

    // Координата конечной точки параболы
    public override Point To
    {
      get => new Point(Width, ComputeY(Width)).Rotate(Angle).Shift(From);
      init => throw new InvalidOperationException();
    }

    // Коэффициент кубичесеой параболы
    public double K { get; init; }

    // Длина проекции параболы на ось X
    public double Width { get; init; }

    // Угол поворота параболы относительно начала координат
    public double Angle { get; init; }

    // Направление параболы (для обратного перехода от дуги к прямой)
    public bool IsReversed { get; init; }

    // Длина кривой кубической параболы
    public override double Length
    {
      get
      {
        var points = GetOrComputePoints();
        return points[^1].position;
      }
    }

    // Создание параболической части пути через значение радиуса кривизны в конце параболы и коэффициент K.
    [Pure]
    public static CubicPart FromRadius(double radius, double k)
    {
      if (radius <= 0)
        throw new ArgumentOutOfRangeException(nameof(radius));

      // вычисляем протяженность параболы по оси X, достигающую заданную кривизну
      var width = 1.0 / (radius * Math.Abs(k) * 6.0);

      // создаем параболу из начала координат с полученной протяженностью
      var cubicPart = new CubicPart(new Point(0, 0), width, 0, IsReversed: false, k);

      var targetRadius = Math.Abs(cubicPart.GetCenter().Y);
      if (targetRadius > radius * 1.1) // оцениваем реальный радиус кривизны в конечной точке параболы
      {
        // при достаточно большом расхождении корректируем коэффициент K
        return FromRadius(radius, k * 1.01);
      }

      return cubicPart;
    }

    // Отражение параболы относительно прямой y=x
    [Pure]
    public CubicPart Flip()
    {
      return this with
      {
        Angle = (Angle + Math.PI / 2).NormalizeAngle(),
        K = -K
      };
    }

    // Установка угла поворота параболы
    [Pure]
    public CubicPart WithAngle(double angle)
    {
      return this with
      {
        Angle = angle.NormalizeAngle()
      };
    }

    // Разворот направления параболы
    [Pure]
    public CubicPart Reverse()
    {
      return this with { IsReversed = !IsReversed };
    }

    // Перемещение точки конца параболы в заданные координаты
    [Pure]
    public CubicPart MoveEndTo(Point point)
    {
      var difference = point.Difference(To);

      return this with {From = From.Shift(difference)};
    }

    // Смещение всех координат параболы
    public override CubicPart Shift(Point delta)
    {
      return this with
      {
        From = From.Shift(delta)
      };
    }

    // Вращение всех координат параболы на угол
    public override CubicPart Rotate(double angle)
    {
      return this with
      {
        From = From.Rotate(angle),
        Angle = Angle + angle
      };
    }

    // Вычисление кубического уравнения для заданного X
    private double ComputeY(double x) => K * x * x * x;

    [Pure]
    public CubicPart Extend()
    {
      var extended = this with {Width = Width * 10};
      return IsReversed ? extended.MoveEndTo(To) : extended;
    }

    // Отражение параболы относительно прямой y=-x
    [Pure]
    public CubicPart Rotate180()
    {
      var normalized = Shift(From.InvertSigns());
      var rotated = normalized with {Angle = Angle + Math.PI};
      return IsReversed ? rotated.MoveEndTo(To) : rotated.Shift(From);
    }

    // Процедура табулирования функции кубической параболы с заданным шагом
    public IEnumerable<Point> Tabulate(double delta)
    {
      if (delta < 0)
        throw new ArgumentOutOfRangeException(nameof(delta));

      var width = Width;
      var count = (int) (width / delta);
      var stepX = width / count;

      if (!IsReversed)
      {
        for (var index = 0; index < count; index++)
        {
          var x = stepX * index;
          var y = ComputeY(x);

          yield return new Point(x, y).Rotate(Angle).Shift(From);
        }
      }
      else
      {
        var maxY = ComputeY(width);

        for (var index = 0; index < count; index++)
        {
          var x = width - stepX * index;
          var y = - ComputeY(x) + maxY;

          yield return new Point(width - x, y).Rotate(Angle).Shift(From);
        }
      }
    }

    private (double position, Point point)[]? myPoints;

    // Табулирование и подсчет длины параболы
    private (double position, Point point)[] GetOrComputePoints()
    {
      if (myPoints == null)
      {
        var previous = new Point(double.NaN, double.NaN);
        var position = 0.0;
        var xs = new List<(double, Point)>();

        foreach (var point in Tabulate(delta: Width / 10000))
        {
          if (previous.X is double.NaN)
          {
            previous = point;
            xs.Add((0.0, point));
          }
          else
          {
            position += previous.DistanceTo(point);
            xs.Add((position, point));
            previous = point;
          }
        }

        myPoints = xs.ToArray();
      }

      return myPoints;
    }

    private sealed class PositionComparer : IComparer<(double, Point)>
    {
      private PositionComparer() { }
      public static IComparer<(double, Point)> Instance { get; } = new PositionComparer();

      public int Compare((double, Point) x, (double, Point) y)
      {
        return x.Item1.CompareTo(y.Item1);
      }
    }

    // Получение координаты точки на параболе по расстоянию от начала параболы
    public override Point GetPoint(double position)
    {
      var points = GetOrComputePoints();

      var resultIndex = Array.BinarySearch(points, (position, default), PositionComparer.Instance);
      if (resultIndex >= 0)
      {
        return points[resultIndex].point;
      }

      var largerThanIndex = ~resultIndex;
      if (largerThanIndex <= points.Length)
      {
        return points[largerThanIndex].point;
      }

      return points[^1].point;
    }

    // Вычисление координат центра окружности радиуса R,
    // касающейся конечной точки параболы, в которой она достигает кривизны 1/R
    public Point GetCenter()
    {
      var x = Width;

      var centerX = (x * (1 - 9.0 * K * K * Math.Pow(x, 4.0))) / 2;
      var centerY = (15 * K * K * Math.Pow(x, 4.0) + 1) / (6 * K * x);

      return new Point(centerX, centerY).Rotate(Angle).Shift(From);
    }
  }
}
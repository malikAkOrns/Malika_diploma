using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Windows;

namespace MalikaDiploma.Models.Parts
{
  public sealed record ClothoidPart : PathPart
  {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public ClothoidPart(Point From, double Length, double Angle, bool Direction, bool IsReversed)
    {
      if (Length < 0)
        throw new ArgumentOutOfRangeException(nameof(Length));

      this.From = From;
      this.Length = Length;
      this.Angle = Angle;
      this.Direction = Direction;
      this.IsReversed = IsReversed;

      EndDelta = Helpers.Fresnel(Length);
    }

    [Pure]
    public static ClothoidPart FromCurvature(Point from, double curvature)
    {
      var length = curvature / Math.PI;
      if (length < 0)
      {
        return new ClothoidPart(from, -length, Angle: 0, Direction: false, IsReversed: false);
      }

      return new ClothoidPart(from, length, Angle: 0, Direction: true, IsReversed: false);
    }

    public override Point From { get; init; }

    public override Point To
    {
      get => GetPoint(Length);
      init => throw new InvalidOperationException();
    }

    public double Angle { get; init; }
    public bool Direction { get; init; }
    public bool IsReversed { get; init; }
    public override double Length { get; }
    public Point EndDelta { get; }

    [Pure]
    public ClothoidPart Expand()
    {
      var expanded = new ClothoidPart(From, Length: 2.5, Angle, Direction, IsReversed);

      return IsReversed ? expanded.MoveEndTo(To) : expanded;
    }

    [Pure]
    public ClothoidPart Flip()
    {
      return this with
      {
        Direction = !Direction
      };
    }

    [Pure]
    public ClothoidPart Reverse()
    {
      return this with
      {
        IsReversed = !IsReversed
      };
    }

    // Перемещение точки конца клотоида в заданные координаты
    [Pure]
    public ClothoidPart MoveEndTo(Point point)
    {
      var difference = point.Difference(To);

      return this with {From = From.Shift(difference)};
    }

    // Установка угла поворота клотоида
    [Pure]
    public ClothoidPart WithAngle(double angle)
    {
      return this with
      {
        Angle = angle.NormalizeAngle()
      };
    }

    public override PathPart Shift(Point delta)
    {
      return this with
      {
        From = From.Shift(delta)
      };
    }

    public override PathPart Rotate(double angle)
    {
      return this with
      {
        From = From.Rotate(angle),
        Angle = Angle + angle
      };
    }

    public override Point GetPoint(double position)
    {
      var p = IsReversed ? Length - position : position;

      var complex = Helpers.Fresnel(p);

      if (Direction)
      {
        if (IsReversed)
        {
          complex.X -= EndDelta.X;
        }
      }
      else
      {
        complex.X = -complex.X;

        if (IsReversed)
        {
          complex.X += EndDelta.X;
        }
      }

      if (IsReversed)
      {
        complex.Y = EndDelta.Y - complex.Y;
      }

      // if (IsReversed)
      // {
      //   complex.X -= EndDelta.X;
      //   complex.Y = EndDelta.Y - complex.Y;
      // }

      return complex.Rotate(Angle).Shift(From);
    }

    [Pure]
    public IEnumerable<Point> Tabulate(double delta)
    {
      if (delta < 0)
        throw new ArgumentOutOfRangeException(nameof(delta));

      var count = Length / delta;

      for (var index = 0; index <= count; index++)
      {
        var p = index * delta;
        yield return GetPoint(p);
      }
    }

    [Pure]
    public Point GetCenter()
    {
      var a = GetPoint(Length - 0.0001);
      var b = GetPoint(Length);
      var c = GetPoint(Length + 0.0001);

      var radius = 1 / (Helpers.TriPointCurvature(a, b, c) / -2);
      var angle = a.AngleBetween2(b);

      var center = b.Move(dx: radius, dy: 0);
      return center.Rotate(b, -angle);
    }

    [Pure]
    public double GetCurvatureAtEnd()
    {
      var point1 = GetPoint(Length - 0.0001);
      var point2 = GetPoint(Length);
      var point3 = GetPoint(Length + 0.0001);

      return Helpers.TriPointCurvature(point1, point2, point3) / -2;
    }
  }
}
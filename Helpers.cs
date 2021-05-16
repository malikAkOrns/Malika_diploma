using System;
using System.Diagnostics.Contracts;
using System.Windows;
using Meta.Numerics.Functions;

namespace MalikaDiploma.Models
{
  // Вспомогательные функции для работы с точками и отрезками на координатной плоскости
  public static class Helpers
  {
    // Смещение точки по осям X и Y
    [Pure]
    public static Point Move(this Point point, double dx, double dy)
    {
      return new Point(point.X + dx, point.Y + dy);
    }

    // Смещение точки по осям X и Y
    [Pure]
    public static Point Shift(this Point point, Point delta)
    {
      return new Point(point.X + delta.X, point.Y + delta.Y);
    }

    // Масштабирование координат точки на заданные коэффиценты
    [Pure]
    public static Point Scale(this Point point, double factorX, double factorY)
    {
      return new Point(point.X * factorX, point.Y * factorY);
    }

    // Вращение точки относительно начала координат на угол 'angle' в радианах
    [Pure]
    public static Point Rotate(this Point point, double angle)
    {
      var cos = Math.Cos(angle);
      var sin = Math.Sin(angle);

      return new Point(
        x: point.X * cos - point.Y * sin,
        y: point.X * sin + point.Y * cos);
    }

    // Вращение точки относительно точки 'origin' на угол 'angle' в радианах
    [Pure]
    public static Point Rotate(this Point point, Point origin, double angle)
    {
      var cos = Math.Cos(angle);
      var sin = Math.Sin(angle);

      var px = point.X - origin.X;
      var py = point.Y - origin.Y;

      return new Point(
        x: px * cos - py * sin + origin.X,
        y: px * sin + py * cos + origin.Y);
    }

    // Вычисление расстояние между двумя точками
    [Pure]
    public static double DistanceTo(this Point point, Point other)
    {
      var dx = point.X - other.X;
      var dy = point.Y - other.Y;

      return Math.Sqrt(dx * dx + dy * dy);
    }

    // Вычисление разницы координат между двумя точками
    [Pure]
    public static Point Difference(this Point point, Point other)
    {
      return new (point.X - other.X, point.Y - other.Y);
    }

    // Инвертирование знака координат X и Y точки
    [Pure]
    public static Point InvertSigns(this Point point)
    {
      return new (-point.X, -point.Y);
    }

    // Вычисление угла прямой заданной двумя точками
    [Pure]
    public static double AngleBetween(this Point point1, Point point2)
    {
      var deltaX = point2.X - point1.X;
      var deltaY = point2.Y - point1.Y;

      return (deltaY >= 0 ? Math.PI : 0.0) - Math.Atan(deltaX / deltaY);
    }

    [Pure]
    public static double AngleBetween2(this Point point1, Point point2)
    {
      var deltaX = point2.X - point1.X;
      var deltaY = point2.Y - point1.Y;

      return Math.Atan(deltaX / deltaY);
    }

    [Pure]
    public static Point Fresnel(double p)
    {
      var complex = AdvancedMath.Fresnel(p);

      return new Point(complex.Im, complex.Re);
    }

    // Вычисление кривизны для трех произвольных точек
    [Pure]
    public static double TriPointCurvature(Point p0, Point p1, Point p2)
    {
      var dx1 = p1.X - p0.X;
      var dy1 = p1.Y - p0.Y;

      var dx2 = p2.X - p0.X;
      var dy2 = p2.Y - p0.Y;

      var area = dx1 * dy2 - dy1 * dx2;

      var len0 = p0.DistanceTo(p1);
      var len1 = p1.DistanceTo(p2);
      var len2 = p2.DistanceTo(p0);

      return 4 * area / (len0 * len1 * len2);
    }

    [Pure] public static double ToDegrees(this double radians) => radians * 180.0 / Math.PI;
    [Pure] public static double ToRadians(this double degrees) => degrees * Math.PI / 180.0;

    [Pure]
    public static double NormalizeAngle(this double radians)
    {
      const double twoPi = Math.PI * 2;

      if (radians is > twoPi or < twoPi)
        return radians % twoPi;

      return radians;
    }
  }
}
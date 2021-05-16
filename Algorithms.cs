using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Windows;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Алгоритмы сглаживания пути
  public static class Algorithms
  {
    // Вычисление угла в радианах для расположения прямой заданной двумя точками парралельно оси Y
    [Pure]
    private static double GetNormalizationAngle(Point point1, Point point2)
    {
      var dy = point2.Y - point1.Y;
      var dx = point2.X - point1.X;

      var sign = dx < 0 ? -1 : 1;
      var psi = -Math.Atan(dy / dx) + sign * Math.PI / 2;

      return psi;
    }

    // Процедура C1 сглаживания пути через три заданные точки с помощью дуги
    [Pure]
    public static Smoothing SmoothC1Arc(Point point1, Point point2, Point point3, double smoothingFactor)
    {
      if (smoothingFactor < 0 || smoothingFactor > 1.0)
        throw new ArgumentOutOfRangeException(nameof(smoothingFactor));

      // находим угол нормализации по точкам 1 и 2
      var psi = GetNormalizationAngle(point1, point2);

      // поворачиваем точки 2 и 3 относительно 1
      // смещаем все точки к началу координат размещая точку 1 в начале координат
      var shift = point1.InvertSigns();
      var point2Normalized = point2.Shift(shift).Rotate(psi);
      var point3Normalized = point3.Shift(shift).Rotate(psi);

      // вычисляем угол между отрезками
      var angleBetweenLines = point2Normalized.AngleBetween(point3Normalized);

      // если угол между отрезками пренебрижимо мал, то не делаем сглаживание
      var noAngle = Math.Abs(angleBetweenLines - Math.PI) < 1e-2;
      if (noAngle)
        return new NoSmoothing(new LinePart(point1, point2), new LinePart(point2, point3));

      // вычисляем максимальный радиус вписанной окружности,
      // он должен не превышать половину длины минимального отрезка
      var lineLength1 = point1.DistanceTo(point2);
      var lineLength2 = point2.DistanceTo(point3);

      // если полученный радиус равен 0, то не делаем сглаживание
      var halfMinLineLength = smoothingFactor * Math.Min(lineLength1, lineLength2) / 2;
      if (halfMinLineLength == 0.0)
        return new NoSmoothing(new LinePart(point1, point2), new LinePart(point2, point3));

      // вычисляем положение центра вписанной окружности
      var halfAngle = angleBetweenLines / 2.0;

      var centerNormalized = new Point(
        x: Math.Tan(halfAngle) * halfMinLineLength,
        y: point2Normalized.Y - halfMinLineLength);

      // вычисляем положения точек пересечения вписанной окружности с отрезками
      var contact1Normalized = new Point(x: 0, y: point2Normalized.Y - halfMinLineLength);
      var contact2Normalized = new Point(
        x: halfMinLineLength * Math.Sin(angleBetweenLines),
        y: point2Normalized.Y - halfMinLineLength * Math.Cos(angleBetweenLines));

      // создаем модель сглаживания исходного, состоящую из { прямой, дуги, прямой }
      var arcSmoothing = new C1Smoothing(
        Line1: new LinePart(new Point(0, 0), contact1Normalized),
        Arc: new ArcPart(centerNormalized, contact1Normalized, contact2Normalized, Direction: Math.Tan(halfAngle) < 0),
        Line2: new LinePart(contact2Normalized, point3Normalized));

      // трансформируем модель сглаженного пути обратно в исходную систему координат
      return arcSmoothing.Rotate(-psi).Shift(point1);
    }

    // Процедура C2 сглаживания пути через три заданные точки с помощью кубических парабол и дуги
    [Pure]
    public static Smoothing SmoothC2Cubic(Point point1, Point point2, Point point3, double smoothingFactor)
    {
      // находим угол нормализации по точкам 1 и 2
      var psi = GetNormalizationAngle(point1, point2);

      // поворачиваем точки 2 и 3 относительно 1
      // смещаем все точки к началу координат размещая точку 1 в начале координат
      var shift = point1.InvertSigns();
      var point2Normalized = point2.Shift(shift).Rotate(psi);
      var point3Normalized = point3.Shift(shift).Rotate(psi);

      // вычисляем угол между отрезками
      var angleBetweenLines = point2Normalized.AngleBetween(point3Normalized);

      // вычисляем максимальный радиус вписанной окружности,
      // он должен не превышать половину длины минимального отрезка
      var halfMinLineLength = (Math.Min(point1.DistanceTo(point2), point2.DistanceTo(point3)) / 2) * smoothingFactor;
      var halfAngle = angleBetweenLines / 2.0;

      var centerNormalized = new Point(
        x: Math.Tan(halfAngle) * halfMinLineLength,
        y: point2Normalized.Y - halfMinLineLength);

      var direction = centerNormalized.X < 0;
      var radius = Math.Abs(centerNormalized.X);
      if (radius == 0)
        return new NoSmoothing(new LinePart(point1, point2), new LinePart(point2, point3));

      // вычисление коэффициента K параболы по заданному пользователем
      // в промежутке от 0 до 1 значению степени гладкости
      var k = (1 - smoothingFactor) * (20 - 0.2) + 0.2;
      if (direction) k = -k;

      // создаем кубическую параболу из центра координат
      // задавая желаемый радиус кривизны в конце параболы и коэффициент K
      var cubicPartFromOrigin = CubicPart.FromRadius(radius, k)
        .Flip(); // отражение параболы относительно прямой y = x

      // вычисляем положение центра окружности,
      // в которую переходит кубическая парабола из начала координат
      var arcCenterForCubicEnd = cubicPartFromOrigin.GetCenter();

      // вычислим насколько надо поднять эту окружность чтобы ее центр пересекся с отрезком,
      // делящим угол между отрезками 1-2 и 2-3 пополам
      var arcCenterBetweenLines = new Point(
        x: arcCenterForCubicEnd.X,
        y: point2Normalized.Y - arcCenterForCubicEnd.X / Math.Tan(halfAngle));
      var deltaY = arcCenterBetweenLines.Y - arcCenterForCubicEnd.Y;

      // поднимаем параболу вместе с окружностью
      var cubic1 = cubicPartFromOrigin with { From = new Point(0, deltaY) };

      // вычисляем точку конца второй параболы
      var cubic2To = cubic1.From.Rotate(point2Normalized, angleBetweenLines);

      // создаем вторую параболу из первой
      var cubic2 = cubicPartFromOrigin
        .Flip() // отражаем её относительно прямой y = x
        .Reverse() // переворачиваем
        .WithAngle(angleBetweenLines - Math.PI / 2) // поворачиваем на угол отрезка 2-3
        .MoveEndTo(cubic2To); // располагаем ее конец в на отрезке 2-3

      // создаем модель сглаживания исходного, состоящую из { прямой, параболы, дуги, параболы, прямой }
      var cubicSmoothing = new C2Smoothing(
        new LinePart(new Point(0, 0), cubic1.From),
        cubic1,
        new ArcPart(arcCenterBetweenLines, cubic1.To, cubic2.From, direction),
        cubic2,
        new LinePart(cubic2.To, point3Normalized)
      );

      // трансформируем модель сглаженного пути обратно в исходную систему координат
      return cubicSmoothing.Rotate(-psi).Shift(point1);
    }

    // Процедура C2 сглаживания пути через три заданные точки с помощью клотоид и дуги
    [Pure]
    public static Smoothing SmoothC2Clothoid(Point point1, Point point2, Point point3, double smoothingFactor)
    {
      // находим угол нормализации по точкам 1 и 2
      var psi = GetNormalizationAngle(point1, point2);

      // поворачиваем точки 2 и 3 относительно 1
      // смещаем все точки к началу координат размещая точку 1 в начале координат
      var shift = point1.InvertSigns();
      var point2Normalized = point2.Shift(shift).Rotate(psi);
      var point3Normalized = point3.Shift(shift).Rotate(psi);

      // вычисляем угол между отрезками
      var angleBetweenLines = point2Normalized.AngleBetween(point3Normalized);

      // вычисляем максимальный радиус вписанной окружности,
      // он должен не превышать половину длины минимального отрезка
      var halfMinLineLength = (Math.Min(point1.DistanceTo(point2), point2.DistanceTo(point3)) / 2) * smoothingFactor;
      var halfAngle = angleBetweenLines / 2.0;

      var centerNormalized = new Point(
        x: Math.Tan(halfAngle) * halfMinLineLength,
        y: point2Normalized.Y - halfMinLineLength);

      var direction = centerNormalized.X < 0;
      var radius = Math.Abs(centerNormalized.X);
      if (radius == 0)
        return new NoSmoothing(new LinePart(point1, point2), new LinePart(point2, point3));

      // создаем клотоиду, достигающую заданную кривизну
      var clothoid1 = ClothoidPart.FromCurvature(new Point(0, 0), curvature: 1 / centerNormalized.X);

      // вычисляем положение центра окружности,
      // в которую переходит клотоида в конце пути
      var arcCenterForCubicEnd = clothoid1.GetCenter();

      // вычислим насколько надо поднять эту окружность чтобы ее центр пересекся с отрезком,
      // делящим угол между отрезками 1-2 и 2-3 пополам
      var arcCenterBetweenLines = new Point(
        x: arcCenterForCubicEnd.X,
        y: point2Normalized.Y - arcCenterForCubicEnd.X / Math.Tan(halfAngle));
      var deltaY = arcCenterBetweenLines.Y - arcCenterForCubicEnd.Y;

      // поднимаем клотоиду вместе с окружностью
      clothoid1 = clothoid1 with { From = new Point(0, clothoid1.From.Y + deltaY) };

      // вычисляем точку конца второй клотоиды
      var cubic2To = clothoid1.From.Rotate(point2Normalized, angleBetweenLines);

      // создаем вторую клотоиду из первой
      var clothoid2 = clothoid1
        .Reverse() // переворачиваем
        .WithAngle(angleBetweenLines - Math.PI) // поворачиваем на угол отрезка 2-3
        .MoveEndTo(cubic2To); // располагаем ее конец в на отрезке 2-3

      // создаем модель сглаживания исходного, состоящую из { прямой, клотоиды, дуги, клотоиды, прямой }
      var clothoidSmoothing = new C2Smoothing(
        new LinePart(new Point(0, 0), clothoid1.From),
        clothoid1,
        new ArcPart(arcCenterBetweenLines, clothoid1.To, clothoid2.From, direction),
        clothoid2,
        new LinePart(clothoid2.To, point3Normalized)
      );

      // трансформируем модель сглаженного пути обратно в исходную систему координат
      return clothoidSmoothing.Rotate(-psi).Shift(point1);
    }

    // Процедура объединения моделей сглаживаний по трем точкам в единый путь
    public static IEnumerable<PathPart> JoinIntersectingSmoothings(this IEnumerable<Smoothing> smoothings)
    {
      LinePart? last = null;

      foreach (var smoothing in smoothings)
      {
        if (last == null)
          yield return smoothing.Line1;
        else
          yield return new LinePart(last.From, smoothing.Line1.To);

        foreach (var variable in smoothing.GetInnerParts())
          yield return variable;

        last = smoothing.Line2;
      }

      if (last != null)
      {
        yield return last;
      }
    }
  }
}
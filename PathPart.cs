using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Windows;

namespace MalikaDiploma.Models.Parts
{
  // Абстрактная часть пути
  public abstract record PathPart
  {
    // Координата начала части пути
    public abstract Point From { get; init; }

    // Координата конца части пути
    public abstract Point To { get; init; }

    // Протяженность части пути
    public abstract double Length { get; }

    // Смещение всех координат части пути
    [Pure] public abstract PathPart Shift(Point delta);

    // Вращение всех координат части пути на угол
    [Pure] public abstract PathPart Rotate(double angle);

    // Получение координаты точки на части пути по расстоянию от начала части
    [Pure] public abstract Point GetPoint(double position);

    // Нахождение координаты точки на пути из нескольких частей по расстоянию от начала
    [Pure]
    public static Point? FindPositionInPath(IEnumerable<PathPart> path, double position)
    {
      foreach (var part in path)
      {
        var partLength = part.Length;
        if (partLength >= position)
        {
          return part.GetPoint(position);
        }

        position -= partLength;
      }

      return null;
    }

    // Получение координат точек на пути из нескольких частей, с заданным шагом
    [Pure]
    public static IEnumerable<(Point point, PathPart part)> Tabulate(IEnumerable<PathPart> path, double delta)
    {
      double position = 0;

      foreach (var pathPart in path)
      {
        var partLength = pathPart.Length;
        while (position < partLength)
        {
          yield return (pathPart.GetPoint(position), pathPart);

          position += delta;
        }

        position -= partLength;
      }
    }

    // Вычисление суммарной протяженности пути из нескольких частей
    [Pure]
    public static double TotalLength(IEnumerable<PathPart> path)
    {
      double length = 0;

      foreach (var pathPart in path)
      {
        length += pathPart.Length;
      }

      return length;
    }
  }
}
using System.Windows;

namespace MalikaDiploma.Models.Parts
{
  // Часть пути - Отрезок
  // Задается координатами начальной и конечной точек
  public sealed record LinePart(Point From, Point To)
    : PathPart
  {
    // Протяженность отрезка
    public override double Length => From.DistanceTo(To);

    // Смещение всех координат отрезка
    public override LinePart Shift(Point delta)
    {
      return new(From.Shift(delta), To.Shift(delta));
    }

    // Вращение всех координат отрезка на угол
    public override LinePart Rotate(double angle)
    {
      return new(From.Rotate(angle), To.Rotate(angle));
    }

    // Получение координаты точки на отрезке по расстоянию от точки начала
    public override Point GetPoint(double position)
    {
      var ratio = position / Length;

      var dx = (To.X - From.X) * ratio;
      var dy = (To.Y - From.Y) * ratio;

      return new Point(From.X + dx, From.Y + dy);
    }
  }
}
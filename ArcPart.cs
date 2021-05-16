using System;
using System.Windows;

namespace MalikaDiploma.Models.Parts
{
  // Часть пути - Дуга
  // Задается координатами центра, начальной и конечной точками туги, а так же направлением вращения
  public sealed record ArcPart(Point Center, Point From, Point To, bool Direction)
    : PathPart
  {
    // Радиус дуги
    public double Radius => Center.DistanceTo(From);

    // Угол дуги
    public double Angle => 2 * Math.Asin(From.DistanceTo(To) / (2 * Radius));
    public double AngleWithDirection => Direction ? Angle : -Angle;

    // Протяженность дуги
    public override double Length => Angle * Radius;

    // Смещение всех координат дуги
    public override ArcPart Shift(Point delta)
    {
      return new(Center.Shift(delta), From.Shift(delta), To.Shift(delta), Direction);
    }

    // Вращение всех координат дуги на угол
    public override ArcPart Rotate(double angle)
    {
      return new(Center.Rotate(angle), From.Rotate(angle), To.Rotate(angle), Direction);
    }

    // Получение координаты точки на дуге по расстоянию от начала дуги
    public override Point GetPoint(double position)
    {
      var ratio = position / Length;

      var angle = AngleWithDirection * ratio;

      return From.Rotate(Center, angle);
    }
  }
}
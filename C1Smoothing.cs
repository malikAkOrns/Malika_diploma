using System.Collections.Generic;
using System.Windows;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Результат сглаживания - C1 сглаживание
  // Состоит из прямолинейного участка, переходящего в дугу окружности, а затем в прямолинейный участок
  public sealed record C1Smoothing(LinePart Line1, ArcPart Arc, LinePart Line2)
    : Smoothing
  {
    // Смещение всех координат результатов сглаживания
    public C1Smoothing Shift(Point delta)
    {
      return new(
        Line1.Shift(delta),
        Arc.Shift(delta),
        Line2.Shift(delta));
    }

    // Вращение всех координат результатов сглаживания на угол
    public C1Smoothing Rotate(double angle)
    {
      return new(
        Line1.Rotate(angle),
        Arc.Rotate(angle),
        Line2.Rotate(angle));
    }

    public override IEnumerable<PathPart> GetInnerParts()
    {
      yield return Arc;
    }
  }
}
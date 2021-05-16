using System.Collections.Generic;
using System.Windows;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Результат сглаживания - C2 сглаживание
  // Состоит из прямолинейного участка, переходящего в переходную кривую,
  // затем в дугу окружности, а затем опять в переходную кривую и прямолинейный участок
  public sealed record C2Smoothing(
      LinePart Line1, PathPart Transition1, ArcPart Arc, PathPart Transition2, LinePart Line2)
    : Smoothing
  {
    // Смещение всех координат результатов сглаживания
    public C2Smoothing Shift(Point delta)
    {
      return new(
        Line1.Shift(delta),
        Transition1.Shift(delta),
        Arc.Shift(delta),
        Transition2.Shift(delta),
        Line2.Shift(delta));
    }

    // Вращение всех координат результатов сглаживания на угол
    public C2Smoothing Rotate(double angle)
    {
      return new(
        Line1.Rotate(angle),
        Transition1.Rotate(angle),
        Arc.Rotate(angle),
        Transition2.Rotate(angle),
        Line2.Rotate(angle));
    }

    public override IEnumerable<PathPart> GetInnerParts()
    {
      yield return Transition1;
      yield return Arc;
      yield return Transition2;
    }
  }
}
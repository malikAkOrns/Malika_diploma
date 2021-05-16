using System.Collections.Generic;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Результат сглаживания - Отсутствие сглаживания
  public sealed record NoSmoothing
    (
      LinePart Line1,
      LinePart Line2
    )
    : Smoothing
  {
    public override IEnumerable<PathPart> GetInnerParts()
    {
      yield break;
    }
  }
}
using System.Collections.Generic;
using MalikaDiploma.Models.Parts;

namespace MalikaDiploma.Models
{
  // Абстрактный результат сглаживания
  public abstract record Smoothing
  {
    // Начальный прямолинейный участок сглаженного пути
    public abstract LinePart Line1 { get; init; }

    // Конечный прямолинейный участок сглаженного пути
    public abstract LinePart Line2 { get; init; }

    // Получение сглаживающих участков пути
    public abstract IEnumerable<PathPart> GetInnerParts();
  }
}
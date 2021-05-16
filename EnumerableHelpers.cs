using System;
using System.Collections.Generic;

namespace MalikaDiploma.Models
{
  // Вспомогательные функции для работы с последовательностями значений
  public static class EnumerableHelpers
  {
    public static IEnumerable<(T first, T second, T third)> Triplets<T>(this IEnumerable<T> source)
    {
      (T first, T second, T third) tuple = default;
      var index = 0;

      foreach (var item in source)
      {
        (tuple.first, tuple.second) = (tuple.second!, tuple.third!);
        tuple.third = item;

        if (index++ >= 2) yield return tuple;
      }
    }

    public static IEnumerable<(T first, T second, T third)> TripletsWithDistance<T>(this IEnumerable<T> source, int distanceBetween)
    {
      if (distanceBetween < 0)
        throw new ArgumentOutOfRangeException(nameof(distanceBetween));

      var ringBuffer = new T[3 + distanceBetween * 2];
      var index = 0;

      foreach (var item in source)
      {
        ringBuffer[index % ringBuffer.Length] = item;

        if (index + 1 >= ringBuffer.Length)
        {
          var prev1 = ringBuffer[(index - distanceBetween - 1) % ringBuffer.Length];
          var prev2 = ringBuffer[(index - distanceBetween * 2 - 2) % ringBuffer.Length];

          yield return (prev2, prev1, item);
        }

        index++;
      }
    }
  }
}
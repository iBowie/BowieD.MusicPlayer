using System;
using System.Collections.Generic;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public static class RandomExtension
    {
        private static readonly Random _random = new();

        public static T Random<T>(this IList<T> list)
        {
            var index = _random.Next(0, list.Count);

            return list[index];
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            List<T> temp = new(list);

            list.Clear();

            while (temp.Count > 0)
            {
                var index = _random.Next(0, temp.Count);

                list.Add(temp[index]);

                temp.RemoveAt(index);
            }
        }
        public static IEnumerable<T> ShuffleLinq<T>(this IReadOnlyCollection<T> list)
        {
            List<T> temp = new(list);

            while (temp.Count > 0)
            {
                var index = _random.Next(0, temp.Count);

                var item = temp[index];

                temp.RemoveAt(index);

                yield return item;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public static class RandomExtension
    {
        private static readonly Random _random = new();

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
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;

namespace CustomRadioStations
{
    static class GeneralHelper
    {
        public static string GetShortcutTargetFile(string shortcutFilePath)
        {
            IWshRuntimeLibrary.IWshShell wsh = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutFilePath);

            if (System.IO.File.Exists(sc.TargetPath))
            {
                return sc.TargetPath;
            }
            else
            {
                return string.Empty;
            }
        }

        public static void ShuffleMe<T>(this IList<T> list)
        {
            Random random = new Random();
            int n = list.Count;

            for (int i = list.Count - 1; i > 1; i--)
            {
                int rnd = random.Next(i + 1);

                T value = list[rnd];
                list[rnd] = list[i];
                list[i] = value;
            }
        }

        public static float LimitToRange(
        this float value, float inclusiveMinimum, float inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        public static T GetNext<T>(this List<T> list, T current)
        {
            var index = list.IndexOf(current);
            return index < list.Count - 1 ? list[index + 1] : list[0];
        }

        public static T GetPrevious<T>(this List<T> list, T current)
        {
            var index = list.IndexOf(current);
            return index > 0 ? list[index - 1] : list[list.Count - 1];
        }

        public static string ColorToHex(Color color)
        {
            return "#" + color.ToArgb().ToString("X");
        }

        public static Color HexToColor(string hex)
        {
            return ColorTranslator.FromHtml(hex);
        }

    }
}

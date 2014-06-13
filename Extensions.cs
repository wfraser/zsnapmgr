using System;
using System.Globalization;

namespace zsnapmgr
{
    static class Extensions
    {
        public static int AsInt(this string str)
        {
            return int.Parse(str, NumberStyles.None, NumberFormatInfo.InvariantInfo);
        }

        public static long AsLong(this string str)
        {
            return long.Parse(str, NumberStyles.None, NumberFormatInfo.InvariantInfo);
        }

        public static int GetWeekOfYear(this DateTime date)
        {
            var dfi = DateTimeFormatInfo.InvariantInfo;
            return dfi.Calendar.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }

        public static string HumanNumber(this long num, string format = "G")
        {
            return HumanNumberHelper(num, 1000, string.Empty, format);
        }

        public static string HumanNumberBinary(this long num, string format = "G")
        {
            return HumanNumberHelper(num, 1024, "i", format);
        }

        private static string HumanNumberHelper(long num, int oneThousand, string prefixAddition, string numberFormat)
        {
            string sign = string.Empty;

            if (num == 0)
            {
                return "0";
            }
            else if (num < 0)
            {
                num = Math.Abs(num);
                sign = "-";
            }

            int magnitude = (int)Math.Floor(Math.Log(num) / Math.Log(oneThousand));

            if (magnitude == 0)
            {
                return num.ToString();
            }

            char[] suffixes = { 'k', 'M', 'G', 'T', 'P', 'E' };

            // Not necessary, as a long can't hold 1000^7 anyway (it would require 70 bits).
            //n = Math.Min(n, suffixes.Length);

            double h = (double)num / Math.Pow(oneThousand, magnitude);

            return string.Format("{0}{1} {2}{3}", sign, h.ToString(numberFormat), suffixes[magnitude - 1], prefixAddition);
        }
    }
}

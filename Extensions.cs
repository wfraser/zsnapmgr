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

        public static int GetWeekOfYear(this DateTime date)
        {
            var dfi = DateTimeFormatInfo.InvariantInfo;
            return dfi.Calendar.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }
    }
}

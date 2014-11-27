namespace Nvelope
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class TimeSpanExtensions
    {
#if !PCL
        /// <summary>
        /// Rounding conversion of TimeSpan to Years
        /// </summary>
        public static int RoundToYears(this TimeSpan source)
        {
            return (source.TotalDays / 365).ConvertTo<decimal>().RoundTo().ConvertTo<int>();
        }
#endif

        public static TimeSpan Multiply(this TimeSpan interval, int times) {
            var result = new TimeSpan(0);
            while (--times >= 0)
            {
                result += interval;
            }
            return result;
        }
    }
}

namespace Siffrum.Ecom.BAL.Base
{
    /// <summary>
    /// Centralized IST (UTC+05:30) date helper.
    /// DB stores timestamps in UTC; all business-logic date comparisons
    /// (today, this week, this month, fresh arrivals) must use IST boundaries.
    /// </summary>
    public static class IstDateHelper
    {
        private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

        /// <summary>Current IST DateTime.</summary>
        public static DateTime Now => DateTime.UtcNow.Add(IstOffset);

        /// <summary>Current IST date (midnight IST, expressed as UTC value).</summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// UTC instant corresponding to the start of an IST day.
        /// E.g. IST 2026-04-25 00:00 → UTC 2026-04-24 18:30.
        /// Use this for DB queries where timestamps are stored in UTC.
        /// </summary>
        public static DateTime IstDayStartUtc(DateTime? istDate = null)
        {
            var day = istDate?.Date ?? Today;
            return DateTime.SpecifyKind(day, DateTimeKind.Utc).Subtract(IstOffset);
        }

        /// <summary>UTC instant corresponding to end of an IST day (exclusive).</summary>
        public static DateTime IstDayEndUtc(DateTime? istDate = null)
        {
            return IstDayStartUtc(istDate).AddDays(1);
        }

        /// <summary>UTC instant for the 1st of the current IST month.</summary>
        public static DateTime IstMonthStartUtc()
        {
            var now = Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            return DateTime.SpecifyKind(firstDay, DateTimeKind.Utc).Subtract(IstOffset);
        }

        /// <summary>UTC instant for N days ago from IST today start.</summary>
        public static DateTime IstDaysAgoUtc(int days)
        {
            return IstDayStartUtc().AddDays(-days);
        }

        /// <summary>
        /// Converts an IST DateTime to UTC.
        /// Treats the input as IST regardless of its Kind property.
        /// </summary>
        /// <param name="istDateTime">DateTime in IST timezone (should have time component)</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime ToUtc(DateTime istDateTime)
        {
            // Strip any timezone info and treat as IST, then convert to UTC
            // IST is UTC+5:30, so UTC = IST - 5:30
            var dt = new DateTime(istDateTime.Ticks, DateTimeKind.Unspecified);
            return dt.Subtract(IstOffset);
        }

        /// <summary>
        /// Converts an IST date+time string components to UTC DateTime.
        /// Use this when you have separate date and time strings in IST.
        /// </summary>
        public static DateTime IstDateTimeToUtc(int year, int month, int day, int hour, int minute, int second = 0)
        {
            var istDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
            return istDateTime.Subtract(IstOffset);
        }
    }
}

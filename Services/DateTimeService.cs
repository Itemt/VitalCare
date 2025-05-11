using System;

namespace CitasEPS.Services
{
    public class DateTimeService : IDateTimeService
    {
        public (DateTime StartOfWeek, DateTime EndOfWeek) GetWeekRange(DateTime date, DayOfWeek startOfWeekDay = DayOfWeek.Monday)
        {
            DateTime baseDate = date.Date;
            int currentDayOfWeek = (int)baseDate.DayOfWeek;
            int startDayOfWeekInt = (int)startOfWeekDay;

            // Adjust currentDayOfWeek for cultures where Sunday is 0
            if (startDayOfWeekInt == 1 && currentDayOfWeek == 0) // If week starts Monday and current is Sunday
            {
                currentDayOfWeek = 7; // Treat Sunday as 7th day if week starts Monday
            }

            int diff = startDayOfWeekInt - currentDayOfWeek;
            if (diff > 0) // If start day is later in the week than current day (e.g. current=Mon, start=Wed)
            {
                 diff -= 7; // Go to previous week's start day
            }
            
            DateTime startOfWeek = baseDate.AddDays(diff);
            DateTime endOfWeek = startOfWeek.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59); // Inclusive end of day

            return (startOfWeek, endOfWeek);
        }

        public DateTime GetNow()
        {
            return DateTime.Now;
        }
    }
} 
namespace CitasEPS.Services
{
    public interface IDateTimeService
    {
        (DateTime StartOfWeek, DateTime EndOfWeek) GetWeekRange(DateTime date, DayOfWeek startOfWeekDay = DayOfWeek.Monday);
        DateTime GetNow(); // For testability
    }
} 
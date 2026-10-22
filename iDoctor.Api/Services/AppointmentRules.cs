using iDoctor.Domain.Entities;

namespace iDoctor.Api.Services;

public static class TajikTime
{
    // Tajikistan is UTC+5 all year (no daylight saving), while the server runs in UTC
    private static readonly TimeSpan Offset = TimeSpan.FromHours(5);

    public static DateTime Now => DateTime.UtcNow + Offset;
    public static DateOnly Today => DateOnly.FromDateTime(Now);
}

public static class AppointmentRules
{
    public const int MinPatientCancelNoticeHours = 2;
    public const int MaxRangeDays = 60;

    public static DateTime StartOf(AppointmentSlot slot) => slot.SlotDate.ToDateTime(slot.StartTime);

    public static bool IsActive(AppointmentStatus status) =>
        status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;
}
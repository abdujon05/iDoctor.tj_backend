namespace iDoctor.Domain.Entities;



public enum SlotStatus
{
    Available,
    Booked,
    Blocked
}

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Completed,
    CancelledByPatient,
    CancelledByDoctor,
    NoShow
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.CancelReason).HasMaxLength(500);

        builder.HasOne(a => a.Slot)
            .WithOne(s => s.Appointment)
            .HasForeignKey<Appointment>(a => a.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.SlotId).IsUnique();

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Clinic)
            .WithMany()
            .HasForeignKey(a => a.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.DoctorId);
        builder.HasIndex(a => a.Status);
    }
}
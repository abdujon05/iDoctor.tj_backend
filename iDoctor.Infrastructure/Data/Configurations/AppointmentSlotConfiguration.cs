using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.ToTable("appointment_slots");

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(s => s.Doctor)
            .WithMany(d => d.Slots)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Clinic)
            .WithMany(c => c.Slots)
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.DoctorId, s.SlotDate, s.StartTime }).IsUnique();
        builder.HasIndex(s => new { s.DoctorId, s.SlotDate });
        builder.HasIndex(s => s.Status);
    }
}
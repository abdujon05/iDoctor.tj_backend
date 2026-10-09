using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class DoctorWorkingHoursConfiguration : IEntityTypeConfiguration<DoctorWorkingHours>
{
    public void Configure(EntityTypeBuilder<DoctorWorkingHours> builder)
    {
        builder.ToTable("doctor_working_hours", t =>
            t.HasCheckConstraint("ck_day_of_week", "\"DayOfWeek\" BETWEEN 0 AND 6"));
            
        builder.HasOne(w => w.Doctor)
            .WithMany(d => d.WorkingHours)
            .HasForeignKey(w => w.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Clinic)
            .WithMany(c => c.WorkingHours)
            .HasForeignKey(w => w.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
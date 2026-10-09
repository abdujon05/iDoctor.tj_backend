using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");

        builder.HasKey(d => d.UserId);

        builder.HasOne(d => d.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(d => d.ConsultationPrice).HasColumnType("numeric(10,2)");
        builder.Property(d => d.AverageRating).HasColumnType("numeric(3,2)").HasDefaultValue(0);
        builder.Property(d => d.PhotoUrl).HasMaxLength(500);

        // Many-to-many: Doctor <-> Specialty
        builder.HasMany(d => d.Specialties)
            .WithMany(s => s.Doctors)
            .UsingEntity(j => j.ToTable("doctor_specialties"));

        // Many-to-many: Doctor <-> Clinic
        builder.HasMany(d => d.Clinics)
            .WithMany(c => c.Doctors)
            .UsingEntity(j => j.ToTable("doctor_clinics"));
    }
}
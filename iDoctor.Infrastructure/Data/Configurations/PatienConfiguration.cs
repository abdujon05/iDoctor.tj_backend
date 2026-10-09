using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Gender).HasMaxLength(10);

        builder.HasOne(p => p.City)
            .WithMany(c => c.Patients)
            .HasForeignKey(p => p.CityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("clinics");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Address).HasMaxLength(300);
        builder.Property(c => c.Phone).HasMaxLength(20);

        builder.HasOne(c => c.City)
            .WithMany(city => city.Clinics)
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
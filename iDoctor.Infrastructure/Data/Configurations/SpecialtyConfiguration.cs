using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.NameRu).HasMaxLength(100);
        builder.Property(s => s.NameTj).HasMaxLength(100);
    }
}
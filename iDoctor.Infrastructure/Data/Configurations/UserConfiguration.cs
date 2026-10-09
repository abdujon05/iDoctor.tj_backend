using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iDoctor.Domain.Entities;

namespace iDoctor.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
    }
}
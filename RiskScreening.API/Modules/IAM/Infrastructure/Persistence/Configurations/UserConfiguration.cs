using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasMaxLength(36)
            .IsRequired();

        // Value Object conversions — stored as plain strings in DB
        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => new Email(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Username)
            .HasConversion(u => u.Value, v => new Username(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Password)
            .HasConversion(p => p.Hash, v => Password.FromHash(v))
            .HasMaxLength(72) // BCrypt max length
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(u => u.LastLoginAt);
        builder.Property(u => u.LockedAt);

        // Many-to-many: User ↔ Role
        builder.HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity(j => j.ToTable("user_roles"));
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Configurations;

public class ScreeningResultConfiguration : IEntityTypeConfiguration<ScreeningResult>
{
    public void Configure(EntityTypeBuilder<ScreeningResult> builder)
    {
        builder.ToTable("screening_results");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(r => r.SupplierId)
            .HasConversion(v => v.Value, v => new SupplierId(v))
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(r => r.SourcesQueried)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.ScreenedAt)
            .IsRequired();

        builder.Property(r => r.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.TotalMatches)
            .HasDefaultValue(0);

        builder.Property(r => r.EntriesJson);

        // Ignore audit fields that don't exist in the screening_results table
        builder.Ignore(r => r.UpdatedAt);
        builder.Ignore(r => r.UpdatedBy);
    }
}
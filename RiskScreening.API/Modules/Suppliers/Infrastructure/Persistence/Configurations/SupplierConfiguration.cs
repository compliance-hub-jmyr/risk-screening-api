using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Converters;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence mapping for <see cref="Supplier"/> in EF Core.
/// </summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    // The custom converter accepts both value objects and raw strings in provider
    // conversion to avoid EF Core 10 casting issues in LIKE/equality predicates.
    private static readonly StringValueObjectConverter<LegalName> LegalNameConverter =
        new(v => v.Value, v => new LegalName(v));

    private static readonly StringValueObjectConverter<CommercialName> CommercialNameConverter =
        new(v => v.Value, v => new CommercialName(v));

    private static readonly StringValueObjectConverter<TaxId> TaxIdConverter = new(v => v.Value, v => new TaxId(v));

    private static readonly StringValueObjectConverter<CountryCode> CountryCodeConverter =
        new(v => v.Value, v => new CountryCode(v));

    /// <summary>
    /// Defines table, key, value object conversions, and column constraints.
    /// </summary>
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(s => s.LegalName)
            .HasConversion(LegalNameConverter)
            .HasMaxLength(LegalName.MaxLength)
            .IsRequired();

        builder.Property(s => s.CommercialName)
            .HasConversion(CommercialNameConverter)
            .HasMaxLength(CommercialName.MaxLength)
            .IsRequired();

        builder.Property(s => s.TaxId)
            .HasConversion(TaxIdConverter)
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(s => s.TaxId)
            .IsUnique();

        builder.Property(s => s.ContactPhone)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new PhoneNumber(v) : null)
            .HasMaxLength(50);

        builder.Property(s => s.ContactEmail)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new Email(v) : null)
            .HasMaxLength(255);

        builder.Property(s => s.Website)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new WebsiteUrl(v) : null)
            .HasMaxLength(500);

        builder.Property(s => s.Address)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new SupplierAddress(v) : null)
            .HasMaxLength(SupplierAddress.MaxLength);

        builder.Property(s => s.Country)
            .HasConversion(CountryCodeConverter)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(s => s.AnnualBillingUsd)
            .HasConversion(
                v => v != null ? v.Value : (decimal?)null,
                v => v.HasValue ? new AnnualBilling(v.Value) : null)
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.Notes);
    }
}
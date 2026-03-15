using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(s => s.LegalName)
            .HasConversion(v => v.Value, v => new LegalName(v))
            .HasMaxLength(LegalName.MaxLength)
            .IsRequired();

        builder.Property(s => s.CommercialName)
            .HasConversion(v => v.Value, v => new CommercialName(v))
            .HasMaxLength(CommercialName.MaxLength)
            .IsRequired();

        builder.Property(s => s.TaxId)
            .HasConversion(v => v.Value, v => new TaxId(v))
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
            .HasConversion(v => v.Value, v => new CountryCode(v))
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
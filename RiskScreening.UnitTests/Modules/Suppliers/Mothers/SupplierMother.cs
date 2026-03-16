using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

namespace RiskScreening.UnitTests.Modules.Suppliers.Mothers;

/// <summary>
///     Object Mother for <see cref="Supplier"/> — named business scenarios.
///     Returns a <see cref="SupplierBuilder"/> so tests can further customize if needed.
/// </summary>
public static class SupplierMother
{
    /// <summary>A standard pending supplier with all fields populated.</summary>
    public static SupplierBuilder Pending()
    {
        return SupplierBuilder.ASupplier();
    }

    /// <summary>A supplier that has been soft-deleted.</summary>
    public static SupplierBuilder Deleted()
    {
        return SupplierBuilder.ASupplier().Deleted();
    }

    /// <summary>A supplier with a specific tax ID.</summary>
    public static SupplierBuilder WithTaxId(string taxId)
    {
        return SupplierBuilder.ASupplier().WithTaxId(taxId);
    }

    /// <summary>A minimal supplier with only required fields.</summary>
    public static SupplierBuilder Minimal()
    {
        return SupplierBuilder.ASupplier().WithNoOptionalFields();
    }
}
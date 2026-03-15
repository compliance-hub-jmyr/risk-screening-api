using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

/// <summary>
///     Supplier aggregate root. Manages due-diligence lifecycle: status transitions,
///     risk level updates, and soft-deletion.
/// </summary>
public class Supplier : AggregateRoot
{
    public LegalName LegalName { get; private set; } = null!;
    public CommercialName CommercialName { get; private set; } = null!;
    public TaxId TaxId { get; private set; } = null!;
    public PhoneNumber? ContactPhone { get; private set; }
    public Email? ContactEmail { get; private set; }
    public WebsiteUrl? Website { get; private set; }
    public SupplierAddress? Address { get; private set; }
    public CountryCode Country { get; private set; } = null!;
    public AnnualBilling? AnnualBillingUsd { get; private set; }
    public RiskLevel RiskLevel { get; private set; } = RiskLevel.None;
    public SupplierStatus Status { get; private set; } = SupplierStatus.Pending;
    public bool IsDeleted { get; private set; }
    public string? Notes { get; private set; }

    private Supplier()
    {
    }
}
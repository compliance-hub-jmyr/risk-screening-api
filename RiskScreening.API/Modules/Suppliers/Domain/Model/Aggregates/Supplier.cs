using RiskScreening.API.Modules.Suppliers.Domain.Exceptions;
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

    public static Supplier Create(
        string legalName,
        string commercialName,
        string taxId,
        string country,
        string? contactPhone = null,
        string? contactEmail = null,
        string? website = null,
        string? address = null,
        decimal? annualBillingUsd = null,
        string? notes = null)
    {
        return new Supplier
        {
            LegalName = new LegalName(legalName),
            CommercialName = new CommercialName(commercialName),
            TaxId = new TaxId(taxId),
            Country = new CountryCode(country),
            ContactPhone = contactPhone is not null ? new PhoneNumber(contactPhone) : null,
            ContactEmail = contactEmail is not null ? new Email(contactEmail) : null,
            Website = website is not null ? new WebsiteUrl(website) : null,
            Address = address is not null ? new SupplierAddress(address) : null,
            AnnualBillingUsd = annualBillingUsd.HasValue ? new AnnualBilling(annualBillingUsd.Value) : null,
            Notes = notes,
            Status = SupplierStatus.Pending,
            RiskLevel = RiskLevel.None,
            IsDeleted = false
        };
    }
    
    public void Delete()
    {
        if (IsDeleted) throw new SupplierAlreadyDeletedException(Id);
        IsDeleted = true;
    }

    public void Approve()
    {
        EnsureNotDeleted();
        Status = SupplierStatus.Approved;
    }

    public void Reject()
    {
        EnsureNotDeleted();
        if (Status == SupplierStatus.Rejected)
            throw new InvalidSupplierStateException(Id, "Supplier is already rejected.");
        Status = SupplierStatus.Rejected;
    }

    public void MarkUnderReview()
    {
        EnsureNotDeleted();
        Status = SupplierStatus.UnderReview;
    }

    public void ApplyScreeningResult(RiskLevel riskLevel)
    {
        RiskLevel = riskLevel;
        if (riskLevel == RiskLevel.High)
            Status = SupplierStatus.UnderReview;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new SupplierAlreadyDeletedException(Id);
    }
}
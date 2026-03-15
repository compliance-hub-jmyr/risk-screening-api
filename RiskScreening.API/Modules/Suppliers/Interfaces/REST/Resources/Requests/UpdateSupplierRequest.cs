using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;

/// <summary>Request body for the supplier update endpoint.</summary>
public record UpdateSupplierRequest(
    [property: SwaggerSchema(
        Description = "Official legal name of the supplier.",
        Nullable = false)]
    string LegalName,
    [property: SwaggerSchema(
        Description = "Commercial or trade name of the supplier.",
        Nullable = false)]
    string CommercialName,
    [property: SwaggerSchema(
        Description = "Tax identification number (e.g. RUC, EIN).",
        Nullable = false)]
    string TaxId,
    [property: SwaggerSchema(
        Description = "ISO 3166-1 alpha-2 country code where the supplier is registered.",
        Nullable = false)]
    string Country,
    [property: SwaggerSchema(
        Description = "Primary contact phone number.",
        Format = "phone",
        Nullable = true)]
    string? ContactPhone,
    [property: SwaggerSchema(
        Description = "Primary contact email address.",
        Format = "email",
        Nullable = true)]
    string? ContactEmail,
    [property: SwaggerSchema(
        Description = "Supplier's website URL.",
        Format = "uri",
        Nullable = true)]
    string? Website,
    [property: SwaggerSchema(
        Description = "Physical or mailing address of the supplier.",
        Nullable = true)]
    string? Address,
    [property: SwaggerSchema(
        Description = "Estimated annual billing amount in USD.",
        Format = "decimal",
        Nullable = true)]
    decimal? AnnualBillingUsd,
    [property: SwaggerSchema(
        Description = "Additional notes or observations about the supplier.",
        Nullable = true)]
    string? Notes
);

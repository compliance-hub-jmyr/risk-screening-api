using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;

/// <summary>Response body representing a supplier entity.</summary>
public record SupplierResponse(
    [property: SwaggerSchema(
        Description = "Unique identifier of the supplier.",
        Format = "uuid",
        Nullable = false)]
    string Id,
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
        Description = "ISO 3166-1 alpha-2 country code where the supplier is registered.",
        Nullable = false)]
    string Country,
    [property: SwaggerSchema(
        Description = "Estimated annual billing amount in USD.",
        Format = "decimal",
        Nullable = true)]
    decimal? AnnualBillingUsd,
    [property: SwaggerSchema(
        Description = "Calculated risk level of the supplier.",
        Nullable = false)]
    string RiskLevel,
    [property: SwaggerSchema(
        Description = "Current workflow status of the supplier.",
        Nullable = false)]
    string Status,
    [property: SwaggerSchema(
        Description = "Indicates whether the supplier has been soft-deleted.",
        Nullable = false)]
    bool IsDeleted,
    [property: SwaggerSchema(
        Description = "Additional notes or observations about the supplier.",
        Nullable = true)]
    string? Notes,
    [property: SwaggerSchema(
        Description = "Timestamp when the supplier was created.",
        Format = "date-time",
        Nullable = false)]
    DateTime CreatedAt,
    [property: SwaggerSchema(
        Description = "Timestamp when the supplier was last updated.",
        Format = "date-time",
        Nullable = false)]
    DateTime UpdatedAt,
    [property: SwaggerSchema(
        Description = "ID of the user who created the supplier.",
        Format = "uuid",
        Nullable = true)]
    string? CreatedBy,
    [property: SwaggerSchema(
        Description = "ID of the user who last updated the supplier.",
        Format = "uuid",
        Nullable = true)]
    string? UpdatedBy
);

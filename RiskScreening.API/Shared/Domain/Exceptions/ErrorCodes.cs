namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Central registry of all error codes in the system.
/// </summary>
/// <remarks>
///     Numbering scheme:
///     <list type="table">
///         <item><term>1000–1999</term><description>Validation &amp; Value Object errors → HTTP 400</description></item>
///         <item><term>2000–2999</term><description>Authentication errors → HTTP 401</description></item>
///         <item><term>3000–3999</term><description>Authorization errors → HTTP 403</description></item>
///         <item><term>4000–4999</term><description>Entity isn't found → HTTP 404</description></item>
///         <item><term>5000–5999</term><description>Business rule violations → HTTP 409</description></item>
///         <item><term>6000–6999</term><description>Infrastructure errors → HTTP 500/502</description></item>
///         <item><term>9000–9999</term><description>Generic/unexpected errors → HTTP 500</description></item>
///     </list>
///     Rule: never remove or reuse a code; only append new ones.
/// </remarks>
public static class ErrorCodes
{
    // =====================================================================
    // VALIDATION ERRORS (1000–1999)
    // =====================================================================

    public const int ValidationFailed = 1000;
    public const string ValidationFailedCode = "VALIDATION_FAILED";

    public const int ConstraintViolation = 1001;
    public const string ConstraintViolationCode = "CONSTRAINT_VIOLATION";

    public const int InvalidArgument = 1002;
    public const string InvalidArgumentCode = "INVALID_ARGUMENT";

    // Generic Value Object error — specific VO errors build their own code via InvalidValueException
    public const int InvalidValue = 1100;
    public const string InvalidValueCode = "INVALID_VALUE";

    public const int InvalidEmail = 1101;
    public const string InvalidEmailCode = "INVALID_EMAIL";

    public const int InvalidUsername = 1102;
    public const string InvalidUsernameCode = "INVALID_USERNAME";

    public const int InvalidPassword = 1103;
    public const string InvalidPasswordCode = "INVALID_PASSWORD";

    public const int InvalidCountryCode = 1104;
    public const string InvalidCountryCodeCode = "INVALID_COUNTRY_CODE";

    public const int InvalidPhoneNumber = 1105;
    public const string InvalidPhoneNumberCode = "INVALID_PHONE_NUMBER";

    public const int InvalidWebsiteUrl = 1106;
    public const string InvalidWebsiteUrlCode = "INVALID_WEBSITE_URL";

    public const int InvalidTaxId = 1107;
    public const string InvalidTaxIdCode = "INVALID_TAX_ID";

    public const int InvalidLegalName = 1108;
    public const string InvalidLegalNameCode = "INVALID_LEGAL_NAME";

    public const int InvalidCommercialName = 1109;
    public const string InvalidCommercialNameCode = "INVALID_COMMERCIAL_NAME";

    public const int InvalidSupplierAddress = 1110;
    public const string InvalidSupplierAddressCode = "INVALID_SUPPLIER_ADDRESS";

    public const int InvalidAnnualBilling = 1111;
    public const string InvalidAnnualBillingCode = "INVALID_ANNUAL_BILLING";

    public const int InvalidSupplierId = 1112;
    public const string InvalidSupplierIdCode = "INVALID_SUPPLIER_ID";

    // =====================================================================
    // AUTHENTICATION ERRORS (2000–2999)
    // =====================================================================

    public const int AuthenticationFailed = 2000;
    public const string AuthenticationFailedCode = "AUTHENTICATION_FAILED";

    public const int InvalidCredentials = 2001;
    public const string InvalidCredentialsCode = "INVALID_CREDENTIALS";

    public const int AccountLocked = 2002;
    public const string AccountLockedCode = "ACCOUNT_LOCKED";

    public const int AccountPendingVerification = 2003;
    public const string AccountPendingVerificationCode = "ACCOUNT_PENDING_VERIFICATION";

    public const int InvalidToken = 2100;
    public const string InvalidTokenCode = "INVALID_TOKEN";

    public const int TokenExpired = 2101;
    public const string TokenExpiredCode = "TOKEN_EXPIRED";

    // =====================================================================
    // AUTHORIZATION ERRORS (3000–3999)
    // =====================================================================

    public const int AuthorizationFailed = 3000;
    public const string AuthorizationFailedCode = "AUTHORIZATION_FAILED";

    public const int PermissionDenied = 3001;
    public const string PermissionDeniedCode = "PERMISSION_DENIED";

    // =====================================================================
    // NOT FOUND ERRORS (4000–4999)
    // =====================================================================

    public const int EntityNotFound = 4000;
    public const string EntityNotFoundCode = "ENTITY_NOT_FOUND";

    public const int SupplierNotFound = 4001;
    public const string SupplierNotFoundCode = "SUPPLIER_NOT_FOUND";

    public const int ScreeningResultNotFound = 4002;
    public const string ScreeningResultNotFoundCode = "SCREENING_RESULT_NOT_FOUND";

    // =====================================================================
    // BUSINESS RULE VIOLATIONS (5000–5999)
    // =====================================================================

    public const int BusinessRuleViolation = 5000;
    public const string BusinessRuleViolationCode = "BUSINESS_RULE_VIOLATION";

    public const int DuplicateEntry = 5001;
    public const string DuplicateEntryCode = "DUPLICATE_ENTRY";

    public const int InvalidOperation = 5002;
    public const string InvalidOperationCode = "INVALID_OPERATION";

    public const int SupplierTaxIdAlreadyExists = 5003;
    public const string SupplierTaxIdAlreadyExistsCode = "SUPPLIER_TAX_ID_ALREADY_EXISTS";

    public const int InvalidSupplierState = 5004;
    public const string InvalidSupplierStateCode = "INVALID_SUPPLIER_STATE";

    public const int SupplierAlreadyDeleted = 5005;
    public const string SupplierAlreadyDeletedCode = "SUPPLIER_ALREADY_DELETED";

    // =====================================================================
    // INFRASTRUCTURE ERRORS (6000–6999)
    // =====================================================================

    public const int InfrastructureError = 6000;
    public const string InfrastructureErrorCode = "INFRASTRUCTURE_ERROR";

    public const int DatabaseError = 6001;
    public const string DatabaseErrorCode = "DATABASE_ERROR";

    public const int DatabaseIntegrityViolation = 6002;
    public const string DatabaseIntegrityViolationCode = "DATABASE_INTEGRITY_VIOLATION";

    public const int RequiredSeedDataMissing = 6003;
    public const string RequiredSeedDataMissingCode = "REQUIRED_SEED_DATA_MISSING";

    // =====================================================================
    // GENERIC ERRORS (9000–9999)
    // =====================================================================

    public const int InternalServerError = 9000;
    public const string InternalServerErrorCode = "INTERNAL_SERVER_ERROR";
}
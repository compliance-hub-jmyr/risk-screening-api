namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Base exception for entities not found in the repository.
///     Maps to HTTP 404 Not Found.
/// </summary>
/// <example>
/// <code>
/// public class RiskScreeningNotFoundException: EntityNotFoundException
/// {
///     public RiskScreeningNotFoundException(Guid id)
///         : base("RiskScreening", id, ErrorCodes.RiskScreeningNotFound, ErrorCodes.RiskScreeningNotFoundCode) { }
/// }
/// </code>
/// </example>
public abstract class EntityNotFoundException : DomainException
{
    /// <summary>Name of the entity that was not found.</summary>
    public string EntityName { get; }

    /// <summary>Field used to look up the entity (e.g. "id", "email").</summary>
    public string Field { get; }

    /// <summary>Value that was searched for.</summary>
    public string Value { get; }

    /// <summary>Lookup by primary key (ID).</summary>
    protected EntityNotFoundException(string entityName, object id, int errorNumber, string errorCode)
        : base($"{entityName} not found with id: {id}", errorNumber, errorCode)
    {
        EntityName = entityName;
        Field = "id";
        Value = id.ToString()!;
    }

    /// <summary>Lookup by a unique field (natural key like name, email).</summary>
    protected EntityNotFoundException(string entityName, string field, string value, int errorNumber, string errorCode)
        : base($"{entityName} not found with {field}: {value}", errorNumber, errorCode)
    {
        EntityName = entityName;
        Field = field;
        Value = value;
    }
}

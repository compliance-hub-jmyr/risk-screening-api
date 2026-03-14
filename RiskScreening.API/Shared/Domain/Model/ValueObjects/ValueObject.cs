namespace RiskScreening.API.Shared.Domain.Model.ValueObjects;

/// <summary>
/// Base record for Value Objects in Domain-Driven Design.
/// Value Objects are immutable and defined by their attributes,
/// not by identity. Equality is structural — two Value Objects
/// with the same data are considered equal.
/// </summary>
public abstract record ValueObject;
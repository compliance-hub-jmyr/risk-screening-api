using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Converters;

/// <summary>
///     Generic EF Core value converter for value objects that wrap a single string.
///     Wraps the toProvider expression so it also accepts raw <see cref="string"/> inputs,
///     fixing the <see cref="InvalidCastException"/> EF Core 10 throws when Sanitize()
///     passes a plain string to a HasConversion-mapped column.
/// </summary>
public class StringValueObjectConverter<TValueObject> : ValueConverter<TValueObject, string>
    where TValueObject : class
{
    public StringValueObjectConverter(
        Expression<Func<TValueObject, string>> toProvider,
        Expression<Func<string, TValueObject>> fromProvider)
        : base(toProvider, fromProvider)
    {
        var compiled = toProvider.Compile();
        ConvertToProvider = value => value switch
        {
            null => null,
            string s => s,
            TValueObject vo => compiled(vo),
            _ => throw new InvalidCastException(
                $"Cannot convert {value.GetType().Name} to provider type for {typeof(TValueObject).Name}.")
        };
    }

    public override Func<object?, object?> ConvertToProvider { get; }
}
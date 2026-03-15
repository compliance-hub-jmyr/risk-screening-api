using FluentValidation;
using RiskScreening.API.Modules.Scraping.Domain.Model.Queries;

namespace RiskScreening.API.Modules.Scraping.Application.Search;

/// <summary>
///     Validates <see cref="SearchRiskListsQuery"/> before the handler executes.
///     <para>
///         Caught by <c>ValidationPipelineBehavior</c> and converted to HTTP 400
///         with field-level errors automatically — no controller logic needed.
///     </para>
/// </summary>
public class SearchRiskListsQueryValidator : AbstractValidator<SearchRiskListsQuery>
{
    private static readonly HashSet<string> ValidSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "ofac", "worldbank", "icij"
    };

    public SearchRiskListsQueryValidator()
    {
        RuleFor(x => x.Term)
            .NotEmpty()
            .WithMessage("Query parameter 'q' is required.");

        RuleForEach(x => x.SourceNames)
            .Must(source => ValidSources.Contains(source))
            .WithMessage("Invalid source '{PropertyValue}'. Valid values: ofac, worldbank, icij.");
    }
}

using MediatR;
using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Domain.Model.Queries;

/// <summary>
///     Query to search one or more risk list sources for a given term.
///     <para>
///         When <see cref="SourceNames"/> is <c>null</c> or empty, all registered
///         <see cref="IScrapingSource"/> implementations are queried in parallel.
///     </para>
/// </summary>
public record SearchRiskListsQuery(
    string Term,
    IReadOnlyList<string>? SourceNames = null) : IRequest<SearchResult>;

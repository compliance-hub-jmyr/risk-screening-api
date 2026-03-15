using MediatR;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;

public record GetSupplierByIdQuery(string Id) : IRequest<Supplier>;
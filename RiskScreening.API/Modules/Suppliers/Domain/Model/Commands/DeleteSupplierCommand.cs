using MediatR;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;

public record DeleteSupplierCommand(string Id) : IRequest;
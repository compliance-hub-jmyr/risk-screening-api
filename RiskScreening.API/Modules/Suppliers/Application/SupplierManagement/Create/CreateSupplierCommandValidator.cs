using FluentValidation;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Create;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty();
        RuleFor(x => x.CommercialName).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}
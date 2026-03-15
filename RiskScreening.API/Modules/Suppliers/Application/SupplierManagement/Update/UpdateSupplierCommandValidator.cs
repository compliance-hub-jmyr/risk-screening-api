using FluentValidation;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;

namespace RiskScreening.API.Modules.Suppliers.Application.SupplierManagement.Update;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LegalName).NotEmpty();
        RuleFor(x => x.CommercialName).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}
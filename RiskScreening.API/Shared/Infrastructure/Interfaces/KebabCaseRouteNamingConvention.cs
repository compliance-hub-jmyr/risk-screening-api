using Microsoft.AspNetCore.Mvc.ApplicationModels;
using RiskScreening.API.Shared.Infrastructure.Extensions;

namespace RiskScreening.API.Shared.Infrastructure.Interfaces;

/// <summary>
///     A controller model convention that automatically converts controller
///     and action route templates to a kebab-case.
/// </summary>
/// <remarks>
///     Register this convention in <c>Program.cs</c> via
///     <c>builder.Services.AddControllers(o => o.Conventions.Add(new KebabCaseRouteNamingConvention()))</c>.
/// </remarks>
/// <example>
///     A controller named <c>RiskScreeningsController</c> with route <c>[Route("api/[controller]")]</c>
///     will be accessible at <c>/api/risk-screenings</c>.
/// </example>
public class KebabCaseRouteNamingConvention : IControllerModelConvention
{
    /// <summary>
    ///     Applies the kebab-case naming convention to all selectors
    ///     in the given controller model, including its actions.
    /// </summary>
    /// <param name="controller">The controller model to modify.</param>
    public void Apply(ControllerModel controller)
    {
        foreach (var selector in controller.Selectors)
            selector.AttributeRouteModel = ReplaceTemplate(selector, controller.ControllerName);

        foreach (var selector in controller.Actions.SelectMany(a => a.Selectors))
            selector.AttributeRouteModel = ReplaceTemplate(selector, controller.ControllerName);
    }

    /// <summary>
    ///     Replaces the <c>[controller]</c> placeholder in the route template
    ///     with the kebab-case version of the controller name.
    /// </summary>
    /// <param name="selector">The selector model containing the route template.</param>
    /// <param name="name">The controller name to convert to a kebab-case.</param>
    /// <returns>
    ///     A new <see cref="AttributeRouteModel"/> with the updated template,
    ///     or <c>null</c> if the selector has no attribute route model.
    /// </returns>
    private static AttributeRouteModel? ReplaceTemplate(SelectorModel selector, string name)
    {
        return selector.AttributeRouteModel != null
            ? new AttributeRouteModel
            {
                Template = selector.AttributeRouteModel.Template?.Replace("[controller]", name.ToKebabCase())
            }
            : null;
    }
}

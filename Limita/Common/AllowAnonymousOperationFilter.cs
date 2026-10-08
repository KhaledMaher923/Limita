using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Limita.Common;

public sealed class AllowAnonymousOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var metadata = context.ApiDescription
            .ActionDescriptor
            .EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
            operation.Security.Clear();
    }
}
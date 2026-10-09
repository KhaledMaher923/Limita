using Microsoft.OpenApi.Models;

namespace Limita.Api.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Limita API", Version = "v1" });

                // Use cases are nested types (Register.Command, Login.Command), so the plain type name is not unique.
                options.CustomSchemaIds(type => type.DeclaringType is null
                ? type.Name
                : $"{type.DeclaringType.Name}.{type.Name}");

                var bearer = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                };

                options.AddSecurityDefinition("Bearer", bearer);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });

            });

            return services;
        }
    }
}

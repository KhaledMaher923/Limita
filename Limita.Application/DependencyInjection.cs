using FluentValidation;
using Limita.Application.Common.Behaviors;
using Limita.Application.Features.Auth;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);

                // Order matters: logging, then validation, then transaction (commands only).
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
                config.AddOpenBehavior(typeof(ValidationBehavior<,>));
                config.AddOpenBehavior(typeof(TransactionBehavior<,>));
            });

            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            services.AddScoped<OtpService>();
            services.AddScoped<TokenIssuer>();

            return services;
        }
    }
}

using Limita.Application.Common.Abstractions;
using Limita.Infrastructure.Identity;
using Limita.Infrastructure.Messaging;
using Limita.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

            // Do not enable EnableRetryOnFailure without redesigning TransactionBehavior: SQL Server retry
            // strategies do not allow user-initiated transactions unless they run inside an execution strategy.
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpCurrentUser>();
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddSingleton(TimeProvider.System); // inject TimeProvider instead of calling DateTime.UtcNow

            // Authentication services
            services.AddSingleton<IPasswordHasher, PasswordHasherService>();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<ISecretHasher, SecretHasher>();
            services.AddSingleton<ISecretGenerator, SecretGenerator>();

            // SMS provider, chosen by the Sms:Provider setting (see docs/SMS.md).
            services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
            var smsProvider = configuration[$"{SmsOptions.SectionName}:Provider"];

            if (string.Equals(smsProvider, "Twilio", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<ISmsSender, TwilioSmsSender>(client =>
                {
                    client.BaseAddress = new Uri("https://api.twilio.com/");
                    client.Timeout = TimeSpan.FromSeconds(10);
                });
            }
            else if (string.Equals(smsProvider, "Console", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<ISmsSender, DevelopmentSmsSender>(); // the API refuses this outside Development
            }
            else
            {
                services.AddSingleton<ISmsSender, NotConfiguredSmsSender>();
            }

            return services;
        }
    }
}

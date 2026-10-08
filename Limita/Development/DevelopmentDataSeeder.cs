using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Limita.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Limita.Development;

public static class DevelopmentDataSeeder
{
    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        const string email = "dev.user@limita.local";

        var user = await dbContext.Users
            .SingleOrDefaultAsync(item => item.Email == email);

        if (user is null)
        {
            user = User.Register(
                "Limita Development User",
                email,
                "+201000000000",
                passwordHasher.Hash("DevOnly#2026"),
                TimeProvider.System.GetUtcNow());

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        var account = await dbContext.Accounts
            .SingleOrDefaultAsync(item => item.UserId == user.Id);

        if (account is null)
        {
            account = Account.Open(
                user.Id,
                "DEV-ACCOUNT-0001",
                AccountType.Current,
                "USD");

            var openingTransaction = account.Credit(
                Money.Of(10000m, "USD"),
                TransactionCategory.General,
                "Development opening balance",
                TimeProvider.System.GetUtcNow());

            dbContext.Accounts.Add(account);
            dbContext.Transactions.Add(openingTransaction);

            await dbContext.SaveChangesAsync();
        }
    }
}
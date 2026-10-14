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
                TimeProvider.System.GetUtcNow().AddDays(-30));

            dbContext.Accounts.Add(account);
            dbContext.Transactions.Add(openingTransaction);

            await dbContext.SaveChangesAsync();
        }

        // Keep a small, repeatable statement history available for the development UI.
        // Each entry goes through Account so the balance and ledger stay in sync.
        var hasMockHistory = await dbContext.Transactions
            .AnyAsync(item => item.AccountId == account.Id && item.Description != null &&
                item.Description.StartsWith("Mock data:"));

        if (!hasMockHistory)
        {
            var now = TimeProvider.System.GetUtcNow();
            var samples = new (int DaysAgo, decimal Amount, bool IsCredit, TransactionCategory Category, string Description)[]
            {
                (25, 3200m, true, TransactionCategory.General, "Monthly salary"),
                (22, 84.50m, false, TransactionCategory.Food, "Groceries at Fresh Market"),
                (19, 42m, false, TransactionCategory.Transport, "Ride share"),
                (16, 125m, false, TransactionCategory.Bills, "Electricity bill"),
                (12, 56.75m, false, TransactionCategory.Food, "Cafe and lunch"),
                (9, 500m, true, TransactionCategory.Transfer, "Transfer from savings"),
                (6, 210m, false, TransactionCategory.Shopping, "Household supplies"),
                (2, 35m, false, TransactionCategory.MobileTopUp, "Mobile top-up")
            };

            foreach (var sample in samples)
            {
                var amount = Money.Of(sample.Amount, "USD");
                var createdAt = now.AddDays(-sample.DaysAgo);
                var transaction = sample.IsCredit
                    ? account.Credit(amount, sample.Category, $"Mock data: {sample.Description}", createdAt)
                    : account.Debit(amount, sample.Category, $"Mock data: {sample.Description}", createdAt);

                dbContext.Transactions.Add(transaction);
            }

            await dbContext.SaveChangesAsync();
        }

        var hasMockCard = await dbContext.Cards.AnyAsync(item => item.AccountId == account.Id);
        if (!hasMockCard)
        {
            dbContext.Cards.Add(Card.Add(
                account.Id,
                CardNetwork.Visa,
                "4242",
                12,
                TimeProvider.System.GetUtcNow().Year + 3,
                user.FullName,
                "dev-token-visa-4242",
                TimeProvider.System.GetUtcNow()));

            await dbContext.SaveChangesAsync();
        }
    }
}

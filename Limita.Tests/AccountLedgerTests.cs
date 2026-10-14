using Limita.Domain.Common;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Xunit;

namespace Limita.Tests;

public sealed class AccountLedgerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreditAndDebit_UpdateBalanceAndCreateMatchingLedgerEntries()
    {
        var account = Account.Open(Guid.NewGuid(), "TEST-ACCOUNT-01", AccountType.Current, "USD");

        var credit = account.Credit(Money.Of(250m, "USD"), TransactionCategory.General, "Opening funds", Now);
        var debit = account.Debit(Money.Of(42.50m, "USD"), TransactionCategory.Food, "Lunch", Now.AddHours(1));

        Assert.Equal(207.50m, account.Balance.Amount);
        Assert.Equal(TransactionType.Credit, credit.Type);
        Assert.Equal(250m, credit.BalanceAfter);
        Assert.Equal(TransactionType.Debit, debit.Type);
        Assert.Equal(207.50m, debit.BalanceAfter);
        Assert.Equal("USD", debit.Amount.Currency);
    }

    [Fact]
    public void Debit_WhenFundsAreInsufficient_ThrowsAndLeavesBalanceUnchanged()
    {
        var account = Account.Open(Guid.NewGuid(), "TEST-ACCOUNT-02", AccountType.Current, "USD");
        account.Credit(Money.Of(20m, "USD"), TransactionCategory.General, "Opening funds", Now);

        Assert.Throws<DomainException>(() => account.Debit(
            Money.Of(20.01m, "USD"), TransactionCategory.Shopping, "Over budget", Now.AddMinutes(1)));

        Assert.Equal(20m, account.Balance.Amount);
    }

    [Fact]
    public void Credit_WithDifferentCurrency_ThrowsAndLeavesBalanceUnchanged()
    {
        var account = Account.Open(Guid.NewGuid(), "TEST-ACCOUNT-03", AccountType.Current, "USD");

        Assert.Throws<DomainException>(() => account.Credit(
            Money.Of(10m, "EUR"), TransactionCategory.General, "Wrong currency", Now));

        Assert.Equal(0m, account.Balance.Amount);
    }
}

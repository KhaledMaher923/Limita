using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Accounts;

public sealed record GetAccountBalanceQuery(Guid AccountId) : IQuery<AccountBalanceDto>;
public sealed record AccountBalanceDto(Guid AccountId, decimal Balance, string Currency, AccountStatus Status);

internal sealed class GetAccountBalanceHandler(IApplicationDbContext db, ICurrentUser user)
    : IQueryHandler<GetAccountBalanceQuery, AccountBalanceDto>
{
    public async Task<Result<AccountBalanceDto>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var account = await db.Accounts.AsNoTracking().Where(a => a.Id == request.AccountId && a.UserId == userId)
            .Select(a => new AccountBalanceDto(a.Id, a.Balance.Amount, a.Balance.Currency, a.Status))
            .SingleOrDefaultAsync(cancellationToken);
        return account is null ? Error.NotFound("Account.NotFound", "The account was not found.") : account;
    }
}

public sealed record AdjustAccountBalanceCommand(Guid AccountId, decimal Amount, bool Credit) : ICommand<AccountBalanceDto>;
internal sealed class AdjustAccountBalanceValidator : AbstractValidator<AdjustAccountBalanceCommand>
{
    public AdjustAccountBalanceValidator() => RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
}

internal sealed class AdjustAccountBalanceHandler(IApplicationDbContext db, ICurrentUser user, TimeProvider time)
    : ICommandHandler<AdjustAccountBalanceCommand, AccountBalanceDto>
{
    public async Task<Result<AccountBalanceDto>> Handle(AdjustAccountBalanceCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Result.Failure<AccountBalanceDto>(Error.Unauthorized("Auth.Required", "Sign-in is required."));
        var account = await db.Accounts.SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);
        if (account is null) return Error.NotFound("Account.NotFound", "The account was not found.");
        try
        {
            var amount = Money.Of(request.Amount, account.Balance.Currency);
            var entry = request.Credit
                ? account.Credit(amount, TransactionCategory.General, "Account balance credit", time.GetUtcNow())
                : account.Debit(amount, TransactionCategory.General, "Account balance debit", time.GetUtcNow());
            db.Transactions.Add(entry);
            return new AccountBalanceDto(account.Id, account.Balance.Amount, account.Balance.Currency, account.Status);
        }
        catch (Exception ex) when (ex is Limita.Domain.Common.DomainException or ArgumentException)
        {
            return Error.BusinessRule("Account.BalanceAdjustmentFailed", ex.Message);
        }
    }
}

public sealed record AddCardCommand(Guid AccountId, CardNetwork Network, string Last4, int ExpiryMonth,
    int ExpiryYear, string HolderName, string ProviderToken) : ICommand<CardDto>;
public sealed record CardDto(Guid Id, Guid AccountId, CardNetwork Network, string MaskedNumber,
    int ExpiryMonth, int ExpiryYear, string HolderName, CardStatus Status);

internal sealed class AddCardValidator : AbstractValidator<AddCardCommand>
{
    public AddCardValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Network).IsInEnum();
        RuleFor(x => x.Last4).Matches("^[0-9]{4}$");
        RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ExpiryYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.HolderName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProviderToken).NotEmpty().MaximumLength(200);
    }
}

internal sealed class AddCardHandler(IApplicationDbContext db, ICurrentUser user, TimeProvider time)
    : ICommandHandler<AddCardCommand, CardDto>
{
    public async Task<Result<CardDto>> Handle(AddCardCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Result.Failure<CardDto>(Error.Unauthorized("Auth.Required", "Sign-in is required."));
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);
        if (account is null) return Error.NotFound("Account.NotFound", "The account was not found.");
        try
        {
            var card = Card.Add(request.AccountId, request.Network, request.Last4, request.ExpiryMonth,
                request.ExpiryYear, request.HolderName, request.ProviderToken, time.GetUtcNow());
            db.Cards.Add(card);
            return CardViews.Map(card);
        }
        catch (Limita.Domain.Common.DomainException ex) { return Error.Validation("Card.Invalid", ex.Message); }
    }
}

public sealed record GetCardsQuery : IQuery<IReadOnlyList<CardDto>>;
internal sealed class GetCardsHandler(IApplicationDbContext db, ICurrentUser user)
    : IQueryHandler<GetCardsQuery, IReadOnlyList<CardDto>>
{
    public async Task<Result<IReadOnlyList<CardDto>>> Handle(GetCardsQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var cards = await (from card in db.Cards.AsNoTracking()
                           join account in db.Accounts.AsNoTracking() on card.AccountId equals account.Id
                           where account.UserId == userId
                           orderby card.Id
                           select new CardDto(card.Id, card.AccountId, card.Network, "•••• •••• •••• " + card.Last4,
                               card.ExpiryMonth, card.ExpiryYear, card.HolderName, card.Status)).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CardDto>>(cards);
    }
}

public sealed record GetCardQuery(Guid CardId) : IQuery<CardDto>;
internal sealed class GetCardHandler(IApplicationDbContext db, ICurrentUser user)
    : IQueryHandler<GetCardQuery, CardDto>
{
    public async Task<Result<CardDto>> Handle(GetCardQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var card = await (from c in db.Cards.AsNoTracking()
                          join a in db.Accounts.AsNoTracking() on c.AccountId equals a.Id
                          where c.Id == request.CardId && a.UserId == userId
                          select new CardDto(c.Id, c.AccountId, c.Network, "•••• •••• •••• " + c.Last4,
                              c.ExpiryMonth, c.ExpiryYear, c.HolderName, c.Status)).SingleOrDefaultAsync(cancellationToken);
        return card is null ? Error.NotFound("Card.NotFound", "The card was not found.") : card;
    }
}

public sealed record DeleteCardCommand(Guid CardId) : ICommand;
internal sealed class DeleteCardHandler(IApplicationDbContext db, ICurrentUser user) : ICommandHandler<DeleteCardCommand>
{
    public async Task<Result> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Result.Failure(Error.Unauthorized("Auth.Required", "Sign-in is required."));
        var card = await (from c in db.Cards
                          join a in db.Accounts on c.AccountId equals a.Id
                          where c.Id == request.CardId && a.UserId == userId
                          select c).SingleOrDefaultAsync(cancellationToken);
        if (card is null) return Result.Failure(Error.NotFound("Card.NotFound", "The card was not found."));
        card.Close();
        return Result.Success();
    }
}

internal static class CardViews
{
    public static CardDto Map(Card c) => new(c.Id, c.AccountId, c.Network, "•••• •••• •••• " + c.Last4,
        c.ExpiryMonth, c.ExpiryYear, c.HolderName, c.Status);
}

public sealed record GetCreditCardStatementQuery(Guid CardId) : IQuery<CreditCardStatementDto>;
public sealed record StatementEntry(Guid Id, TransactionType Type, decimal Amount, decimal BalanceAfter,
    string Currency, string? Description, DateTimeOffset CreatedAt);
public sealed record CreditCardStatementDto(Guid CardId, Guid AccountId, string Currency, decimal CurrentBalance,
    IReadOnlyList<StatementEntry> Entries);

internal sealed class GetCreditCardStatementHandler(IApplicationDbContext db, ICurrentUser user)
    : IQueryHandler<GetCreditCardStatementQuery, CreditCardStatementDto>
{
    public async Task<Result<CreditCardStatementDto>> Handle(GetCreditCardStatementQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var accountInfo = await (from c in db.Cards.AsNoTracking()
                                 join a in db.Accounts.AsNoTracking() on c.AccountId equals a.Id
                                 where c.Id == request.CardId && a.UserId == userId
                                 select new { c.Id, Account = a }).SingleOrDefaultAsync(cancellationToken);
        if (accountInfo is null) return Error.NotFound("Card.NotFound", "The card was not found.");
        var entries = await db.Transactions.AsNoTracking().Where(t => t.AccountId == accountInfo.Account.Id)
            .OrderByDescending(t => t.CreatedAt).Take(100)
            .Select(t => new StatementEntry(t.Id, t.Type, t.Amount.Amount, t.BalanceAfter, t.Amount.Currency, t.Description, t.CreatedAt))
            .ToListAsync(cancellationToken);
        return new CreditCardStatementDto(accountInfo.Id, accountInfo.Account.Id,
            accountInfo.Account.Balance.Currency, accountInfo.Account.Balance.Amount, entries);
    }
}

public sealed record RepayCreditCardCommand(Guid CardId, Guid SourceAccountId, decimal Amount) : ICommand<AccountBalanceDto>;
internal sealed class RepayCreditCardValidator : AbstractValidator<RepayCreditCardCommand>
{
    public RepayCreditCardValidator() => RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
}
internal sealed class RepayCreditCardHandler(IApplicationDbContext db, ICurrentUser user, TimeProvider time)
    : ICommandHandler<RepayCreditCardCommand, AccountBalanceDto>
{
    public async Task<Result<AccountBalanceDto>> Handle(RepayCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var target = await (from c in db.Cards
                            join a in db.Accounts on c.AccountId equals a.Id
                            where c.Id == request.CardId && a.UserId == userId && c.Status == CardStatus.Active
                            select a).SingleOrDefaultAsync(cancellationToken);
        var source = await db.Accounts.SingleOrDefaultAsync(a => a.Id == request.SourceAccountId && a.UserId == userId, cancellationToken);
        if (target is null) return Error.NotFound("Card.NotFound", "An active card was not found.");
        if (source is null) return Error.NotFound("Account.NotFound", "The funding account was not found.");
        if (target.Id == source.Id) return Error.Validation("Repayment.SameAccount", "Choose a different funding account.");
        if (target.Balance.Currency != source.Balance.Currency) return Error.Validation("Repayment.CurrencyMismatch", "The accounts must use the same currency.");
        try
        {
            var amount = Money.Of(request.Amount, source.Balance.Currency);
            if (!source.HasSufficientFunds(amount)) return Error.BusinessRule("Account.InsufficientFunds", "The funding account has insufficient funds.");
            var now = time.GetUtcNow();
            var debit = source.Debit(amount, TransactionCategory.Bills, "Credit card repayment", now);
            var credit = target.Credit(amount, TransactionCategory.Bills, "Credit card repayment", now);
            db.Transactions.AddRange(debit, credit);
            return new AccountBalanceDto(target.Id, target.Balance.Amount, target.Balance.Currency, target.Status);
        }
        catch (Limita.Domain.Common.DomainException ex) { return Error.BusinessRule("Repayment.Invalid", ex.Message); }
    }
}

public sealed record GetHomeDashboardQuery : IQuery<HomeDashboardDto>;
public sealed record CurrencyBalanceDto(string Currency, decimal Balance);
public sealed record HomeDashboardDto(IReadOnlyList<CurrencyBalanceDto> BalancesByCurrency, int AccountCount,
    int ActiveCardCount, IReadOnlyList<GetAccounts.AccountSummary> Accounts, IReadOnlyList<CardDto> Cards);
internal sealed class GetHomeDashboardHandler(IApplicationDbContext db, ICurrentUser user)
    : IQueryHandler<GetHomeDashboardQuery, HomeDashboardDto>
{
    public async Task<Result<HomeDashboardDto>> Handle(GetHomeDashboardQuery request, CancellationToken cancellationToken)
    {
        if (user.UserId is not Guid userId) return Error.Unauthorized("Auth.Required", "Sign-in is required.");
        var accounts = await db.Accounts.AsNoTracking().Where(a => a.UserId == userId).OrderBy(a => a.AccountNumber)
            .Select(a => new GetAccounts.AccountSummary(a.Id, a.AccountNumber, a.Type, a.Balance.Currency, a.Balance.Amount, a.Status))
            .ToListAsync(cancellationToken);
        var cards = await (from c in db.Cards.AsNoTracking()
                           join a in db.Accounts.AsNoTracking() on c.AccountId equals a.Id
                           where a.UserId == userId && c.Status != CardStatus.Closed
                           select new CardDto(c.Id, c.AccountId, c.Network, "•••• •••• •••• " + c.Last4,
                               c.ExpiryMonth, c.ExpiryYear, c.HolderName, c.Status)).ToListAsync(cancellationToken);
        var balances = accounts.GroupBy(a => a.Currency)
            .Select(group => new CurrencyBalanceDto(group.Key, group.Sum(a => a.Balance)))
            .OrderBy(balance => balance.Currency)
            .ToList();
        return new HomeDashboardDto(balances, accounts.Count, cards.Count(c => c.Status == CardStatus.Active), accounts, cards);
    }
}

internal sealed class CardAddedNotificationHandler(IApplicationDbContext db, TimeProvider time)
    : INotificationHandler<Limita.Application.Common.DomainEvents.DomainEventNotification<Limita.Domain.Events.CardAddedEvent>>
{
    public async Task Handle(Limita.Application.Common.DomainEvents.DomainEventNotification<Limita.Domain.Events.CardAddedEvent> notification, CancellationToken cancellationToken)
    {
        var ownerId = await db.Accounts.Where(a => a.Id == notification.DomainEvent.AccountId)
            .Select(a => (Guid?)a.UserId).SingleOrDefaultAsync(cancellationToken);
        if (ownerId is Guid userId)
            db.Notifications.Add(Notification.Create(userId, NotificationType.CardAdded, "Card added", "A payment card was added to your account.", notification.DomainEvent.CardId, time.GetUtcNow()));
    }
}

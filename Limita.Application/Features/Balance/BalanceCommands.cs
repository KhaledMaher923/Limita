using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Balance.Commands;

public sealed record DebitAccountCommand(Guid AccountId, decimal Amount, string Currency, string Description) : IRequest<Result<Guid>>;

internal sealed class DebitAccountCommandHandler : IRequestHandler<DebitAccountCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DebitAccountCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(DebitAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId && a.UserId == userId, cancellationToken);

        if (account is null)
            return Result.Failure<Guid>(Error.NotFound("Account.NotFound", "Account not found."));

        if (account.Status != AccountStatus.Active)
            return Result.Failure<Guid>(Error.BusinessRule("Account.NotActive", "Account is not active."));

        var amount = Money.Of(command.Amount, command.Currency);

        if (!account.HasSufficientFunds(amount))
            return Result.Failure<Guid>(Error.BusinessRule("InsufficientFunds", "Insufficient funds."));

        var now = _timeProvider.GetUtcNow();

        var transaction = account.Debit(amount, TransactionCategory.General, command.Description, now);

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(transaction.Id);
    }
}

public sealed record CreditAccountCommand(Guid AccountId, decimal Amount, string Currency, string Description) : IRequest<Result<Guid>>;

internal sealed class CreditAccountCommandHandler : IRequestHandler<CreditAccountCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreditAccountCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(CreditAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId && a.UserId == userId, cancellationToken);

        if (account is null)
            return Result.Failure<Guid>(Error.NotFound("Account.NotFound", "Account not found."));

        if (account.Status != AccountStatus.Active)
            return Result.Failure<Guid>(Error.BusinessRule("Account.NotActive", "Account is not active."));

        var amount = Money.Of(command.Amount, command.Currency);

        var now = _timeProvider.GetUtcNow();

        var transaction = account.Credit(amount, TransactionCategory.General, command.Description, now);

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(transaction.Id);
    }
}

public sealed record CheckSufficientFundsQuery(Guid AccountId, decimal Amount, string Currency) : IRequest<Result<bool>>;

internal sealed class CheckSufficientFundsQueryHandler : IRequestHandler<CheckSufficientFundsQuery, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CheckSufficientFundsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(CheckSufficientFundsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);

        if (account is null)
            return Result.Failure<bool>(Error.NotFound("Account.NotFound", "Account not found."));

        var amount = Money.Of(request.Amount, request.Currency);
        return Result.Success(account.HasSufficientFunds(amount));
    }
}
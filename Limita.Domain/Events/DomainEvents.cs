using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Events
{
    /// <summary>Raised for every ledger entry. Spending limits and notifications react to this.</summary>
    public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    TransactionType Type,
    Money Amount,
    TransactionCategory Category) : DomainEvent;

    public sealed record TransferCompletedEvent(
    Guid TransferId,
    Guid FromAccountId,
    Guid ToAccountId,
    Money Amount) : DomainEvent;

    public sealed record CardAddedEvent(Guid CardId, Guid AccountId) : DomainEvent;

    public sealed record SpendingLimitExceededEvent(
    Guid SpendingLimitId,
    Guid UserId,
    Money Limit,
    Money SpentSoFar) : DomainEvent;

    public sealed record SavingsGoalReachedEvent(Guid SavingsGoalId, Guid UserId, string GoalName) : DomainEvent;

}

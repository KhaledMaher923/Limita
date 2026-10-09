using Limita.Domain.Common;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using System;

namespace Limita.Domain.Events
{
    /// <summary>Raised when a bill is paid. The Payment Successful notification reacts to this.</summary>
    public sealed record BillPaidEvent(
        Guid BillPaymentId,
        Guid UserId,
        BillType BillType,
        Money Amount) : DomainEvent;

    /// <summary>Raised when a mobile top-up completes. The phone number is left out on purpose.</summary>
    public sealed record MobileTopUpCompletedEvent(
        Guid MobileTopUpId,
        Guid UserId,
        Money Amount) : DomainEvent;
}

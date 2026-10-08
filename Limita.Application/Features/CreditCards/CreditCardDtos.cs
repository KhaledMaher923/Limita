using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.CreditCards;

public sealed record CreditCardStatementDto(
    Guid CardId,
    string Last4,
    decimal CurrentBalance,
    string Currency,
    DateTimeOffset StatementDate,
    DateTimeOffset DueDate,
    decimal MinimumPayment,
    List<StatementTransactionDto> Transactions
);

public sealed record StatementTransactionDto(
    Guid Id,
    DateTimeOffset Date,
    string Description,
    decimal Amount,
    string Type
);

public sealed record RepayCreditCardRequest(
    Guid CardId,
    Guid FromAccountId,
    decimal Amount
);

public sealed record RepayCreditCardResponse(
    Guid TransactionId,
    decimal NewBalance
);

public sealed record ValidateRepaymentRequest(
    Guid CardId,
    decimal Amount
);

public sealed record ValidateRepaymentResponse(
    bool IsValid,
    string? ErrorMessage,
    decimal MinimumPayment,
    decimal CurrentBalance
);
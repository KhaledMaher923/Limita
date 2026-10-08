using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Home;

public sealed record HomeDashboardDto(
    decimal TotalBalance,
    string Currency,
    List<AccountSummaryDto> Accounts,
    List<CardSummaryDto> Cards,
    List<RecentTransactionDto> RecentTransactions
);

public sealed record AccountSummaryDto(
    Guid Id,
    string AccountNumber,
    string Type,
    decimal Balance,
    string Currency
);

public sealed record CardSummaryDto(
    Guid Id,
    string Last4,
    string Network,
    string Status
);

public sealed record RecentTransactionDto(
    Guid Id,
    Guid AccountId,
    DateTimeOffset Date,
    string Description,
    decimal Amount,
    string Type,
    string Category
);
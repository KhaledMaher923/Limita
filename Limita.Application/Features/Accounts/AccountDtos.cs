using Limita.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Accounts;

public sealed record AccountDto(
    Guid Id,
    string AccountNumber,
    string Type,
    string Status,
    decimal Balance,
    string Currency
);

public sealed record AccountBalanceDto(
    decimal Balance,
    string Currency
);
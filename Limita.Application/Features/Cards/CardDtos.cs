using Limita.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Application.Features.Cards;

public sealed record CardDto(
    Guid Id,
    Guid AccountId,
    string Network,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string HolderName,
    string Status,
    bool IsExpired
);

public sealed record AddCardRequest(
    Guid AccountId,
    CardNetwork Network,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    string HolderName,
    string Token
);

public sealed record AddCardResponse(Guid CardId);
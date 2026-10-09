using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Queries;

public sealed record GetBeneficiariesQuery : IQuery<IReadOnlyList<BeneficiaryDto>>;

public sealed record BeneficiaryDto(
    Guid Id,
    string Name,
    string TransferType,
    string? MaskedCardNumber,
    string? AccountNumber,
    Guid? BankId,
    Guid? BranchId,
    DateTimeOffset CreatedAt);

internal sealed class GetBeneficiariesHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetBeneficiariesQuery, IReadOnlyList<BeneficiaryDto>>
{
    public async Task<Result<IReadOnlyList<BeneficiaryDto>>> Handle(
        GetBeneficiariesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return Result.Failure<IReadOnlyList<BeneficiaryDto>>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));

        var beneficiaries = await dbContext.Beneficiaries
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BeneficiaryDto(
                b.Id,
                b.Name,
                b.TransferType.ToString(),
                b.MaskedCardNumber,
                b.AccountNumber,
                b.BankId,
                b.BranchId,
                b.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BeneficiaryDto>>(beneficiaries);
    }
}

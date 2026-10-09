using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Queries;

public sealed record GetBranchesOfBankQuery(Guid BankId) : IQuery<IReadOnlyList<BranchDto>>;

public sealed record BranchDto(Guid Id, string Name, string Address);

public sealed class GetBranchesOfBankValidator : AbstractValidator<GetBranchesOfBankQuery>
{
    public GetBranchesOfBankValidator()
    {
        RuleFor(x => x.BankId).NotEmpty();
    }
}

internal sealed class GetBranchesOfBankHandler(
    IApplicationDbContext dbContext)
    : IQueryHandler<GetBranchesOfBankQuery, IReadOnlyList<BranchDto>>
{
    public async Task<Result<IReadOnlyList<BranchDto>>> Handle(
        GetBranchesOfBankQuery request,
        CancellationToken cancellationToken)
    {
        var bankExists = await dbContext.Banks
            .AnyAsync(b => b.Id == request.BankId, cancellationToken);

        if (!bankExists)
            return Result.Failure<IReadOnlyList<BranchDto>>(
                Error.NotFound("Bank.NotFound", "The bank was not found."));

        var branches = await dbContext.BankBranches
            .AsNoTracking()
            .Where(b => b.BankId == request.BankId)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name, b.Address))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BranchDto>>(branches);
    }
}

using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Transfers.Queries;

public sealed record GetBanksQuery : IQuery<IReadOnlyList<BankDto>>;

public sealed record BankDto(Guid Id, string Name, string SwiftCode);

internal sealed class GetBanksHandler(
    IApplicationDbContext dbContext)
    : IQueryHandler<GetBanksQuery, IReadOnlyList<BankDto>>
{
    public async Task<Result<IReadOnlyList<BankDto>>> Handle(
        GetBanksQuery request,
        CancellationToken cancellationToken)
    {
        var banks = await dbContext.Banks
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BankDto(b.Id, b.Name, b.SwiftCode))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BankDto>>(banks);
    }
}

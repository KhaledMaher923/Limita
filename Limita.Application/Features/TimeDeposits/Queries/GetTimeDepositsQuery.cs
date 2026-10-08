using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.TimeDeposits.Queries;

public sealed record GetTimeDepositsQuery
    : IQuery<IReadOnlyList<TimeDepositDto>>;

public sealed record TimeDepositDto(
    Guid Id,
    decimal Principal,
    string Currency,
    int TermInMonths,
    decimal AnnualInterestRatePercent,
    decimal MaturityAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset MaturesAt,
    string Status);

internal sealed class GetTimeDepositsHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IQueryHandler<GetTimeDepositsQuery, IReadOnlyList<TimeDepositDto>>
{
    public async Task<Result<IReadOnlyList<TimeDepositDto>>> Handle(
        GetTimeDepositsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<TimeDepositDto>>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var deposits = await dbContext.TimeDeposits
            .AsNoTracking()
            .Where(deposit => deposit.UserId == userId)
            .OrderByDescending(deposit => deposit.CreatedAt)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();

        IReadOnlyList<TimeDepositDto> response = deposits
            .Select(deposit =>
            {
                var status = deposit.Status;

                if (status == TimeDepositStatus.Active
                    && now >= deposit.MaturesAt)
                {
                    status = TimeDepositStatus.Matured;
                }

                return new TimeDepositDto(
                    deposit.Id,
                    deposit.Principal.Amount,
                    deposit.Principal.Currency,
                    deposit.TermInMonths,
                    deposit.AnnualInterestRatePercent,
                    deposit.CalculateMaturityAmount().Amount,
                    deposit.CreatedAt,
                    deposit.MaturesAt,
                    status.ToString());
            })
            .ToList();

        return Result.Success<IReadOnlyList<TimeDepositDto>>(response);
    }
}
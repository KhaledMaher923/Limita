using Limita.Application.Common;
using Limita.Application.Common.Messaging;

namespace Limita.Application.Features.TimeDeposits.Queries;

public sealed record GetTimeDepositTermsQuery
    : IQuery<IReadOnlyList<DepositTermDto>>;

public sealed record DepositTermDto(
    int TermInMonths,
    decimal AnnualInterestRatePercent,
    decimal MinimumAmount);

internal sealed class GetTimeDepositTermsHandler
    : IQueryHandler<GetTimeDepositTermsQuery, IReadOnlyList<DepositTermDto>>
{
    public Task<Result<IReadOnlyList<DepositTermDto>>> Handle(
        GetTimeDepositTermsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepositTermDto> terms = DepositTermCatalog.Terms
            .Select(term => new DepositTermDto(
                term.TermInMonths,
                term.AnnualInterestRatePercent,
                DepositTermCatalog.MinimumPrincipal))
            .ToList();

        return Task.FromResult(
            Result.Success<IReadOnlyList<DepositTermDto>>(terms));
    }
}
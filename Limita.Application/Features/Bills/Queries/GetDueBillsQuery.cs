using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Bills.Queries;

/// <summary>The signed-in customer's unpaid bills that fall due within the next DaysAhead days (Smart Pay).</summary>
public sealed record GetDueBillsQuery(int DaysAhead = 7) : IQuery<IReadOnlyList<BillDto>>;

public sealed class GetDueBillsValidator : AbstractValidator<GetDueBillsQuery>
{
    public GetDueBillsValidator()
    {
        RuleFor(query => query.DaysAhead).InclusiveBetween(0, 90);
    }
}

internal sealed class GetDueBillsHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IQueryHandler<GetDueBillsQuery, IReadOnlyList<BillDto>>
{
    public async Task<Result<IReadOnlyList<BillDto>>> Handle(
        GetDueBillsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<BillDto>>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        var until = DateOnly
            .FromDateTime(timeProvider.GetUtcNow().UtcDateTime)
            .AddDays(request.DaysAhead);

        var bills = await dbContext.Bills
            .AsNoTracking()
            .Where(bill => bill.CustomerUserId == userId
                && bill.Status == BillStatus.Unpaid
                && bill.DueDate <= until)
            .OrderBy(bill => bill.DueDate)
            .ToListAsync(cancellationToken);

        IReadOnlyList<BillDto> response = bills.Select(bill => bill.ToDto()).ToList();

        return Result.Success(response);
    }
}

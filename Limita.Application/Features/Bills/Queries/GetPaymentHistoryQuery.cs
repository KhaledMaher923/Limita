using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Bills.Queries;

public sealed record GetPaymentHistoryQuery(
    BillType? Type = null,
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<BillPaymentDto>>;

public sealed record BillPaymentDto(
    Guid Id,
    string Type,
    string? Company,
    decimal Amount,
    string Currency,
    DateTimeOffset PaidAt);

public sealed class GetPaymentHistoryValidator : AbstractValidator<GetPaymentHistoryQuery>
{
    public GetPaymentHistoryValidator()
    {
        RuleFor(query => query.Type)
            .IsInEnum()
            .When(query => query.Type.HasValue);

        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

internal sealed class GetPaymentHistoryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetPaymentHistoryQuery, IReadOnlyList<BillPaymentDto>>
{
    public async Task<Result<IReadOnlyList<BillPaymentDto>>> Handle(
        GetPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<BillPaymentDto>>(
                Error.Unauthorized("Auth.Required", "Sign-in is required."));
        }

        IQueryable<BillPayment> payments = dbContext.BillPayments
            .AsNoTracking()
            .Where(payment => payment.UserId == userId);

        if (request.Type is { } type)
            payments = payments.Where(payment => payment.BillType == type);

        var page = await payments
            .OrderByDescending(payment => payment.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // The company lives on the bill, so it is loaded with a second query instead of a join.
        var billIds = page.Select(payment => payment.BillId).Distinct().ToList();

        var companies = await dbContext.Bills
            .AsNoTracking()
            .Where(bill => billIds.Contains(bill.Id))
            .ToDictionaryAsync(bill => bill.Id, bill => bill.Company, cancellationToken);

        IReadOnlyList<BillPaymentDto> response = page
            .Select(payment => new BillPaymentDto(
                payment.Id,
                payment.BillType.ToString(),
                companies.GetValueOrDefault(payment.BillId),
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.CreatedAt))
            .ToList();

        return Result.Success(response);
    }
}

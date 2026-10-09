using FluentValidation;
using Limita.Application.Common;
using Limita.Application.Common.Abstractions;
using Limita.Application.Common.Messaging;
using Limita.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Limita.Application.Features.Bills.Queries;

public sealed record GetBillByCodeQuery(BillType Type, string Code) : IQuery<BillDto>;

public sealed class GetBillByCodeValidator : AbstractValidator<GetBillByCodeQuery>
{
    public GetBillByCodeValidator()
    {
        RuleFor(query => query.Type).IsInEnum();

        RuleFor(query => query.Code)
            .NotEmpty()
            .MaximumLength(50);
    }
}

internal sealed class GetBillByCodeHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetBillByCodeQuery, BillDto>
{
    public async Task<Result<BillDto>> Handle(
        GetBillByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();

        var bill = await dbContext.Bills
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Type == request.Type && item.Code == code,
                cancellationToken);

        if (bill is null)
        {
            return Result.Failure<BillDto>(
                Error.NotFound("Bill.NotFound", "No bill was found for this code."));
        }

        return Result.Success(bill.ToDto());
    }
}

using Limita.Application.Common;
using Limita.Application.Common.Messaging;

namespace Limita.Application.Features.Transfers.Queries;

public sealed record GetTransferFeeQuery : IQuery<TransferFeeDto>;

public sealed record TransferFeeDto(decimal Fee, string Description);

internal sealed class GetTransferFeeHandler
    : IQueryHandler<GetTransferFeeQuery, TransferFeeDto>
{
    public Task<Result<TransferFeeDto>> Handle(
        GetTransferFeeQuery request,
        CancellationToken cancellationToken)
    {
        var dto = new TransferFeeDto(
            TransferSettings.TransferFeeAmount,
            "Flat transfer fee applied to all transfer types.");

        return Task.FromResult(Result.Success(dto));
    }
}

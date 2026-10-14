using Limita.Api.Common;
using Limita.Application.Features.Transfers.Commands;
using Limita.Application.Features.Transfers.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Api.Controllers
{
    [Route("api/beneficiaries")]
    public sealed class BeneficiariesController(ISender sender) : ApiControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetBeneficiaries(CancellationToken ct)
           => FromResult(await sender.Send(new GetBeneficiariesQuery(), ct));

        [HttpPost]
        public async Task<IActionResult> AddBeneficiary([FromBody] AddBeneficiaryCommand command, CancellationToken ct)
            => FromResult(await sender.Send(command, ct));

    }
}

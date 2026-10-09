using Limita.Api.Common;
using Limita.Application.Features.Accounts;
using Limita.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Limita.Api.Controllers
{
    public sealed class AccountsController(ISender sender) : ApiControllerBase
    {
        /// <summary>GET /api/v1/accounts: the signed-in user's accounts.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
            => FromResult(await sender.Send(new GetAccounts.Query(), cancellationToken));

        /// <summary>POST /api/v1/accounts: opens a new account for the signed-in user.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken cancellationToken)
        {
            var result = await sender.Send(new CreateAccount.Command(request.Type, request.Currency), cancellationToken);

            return FromResult(result, account => Created($"/api/v1/accounts/{account.Id}", account));
        }

        public sealed record CreateAccountRequest(AccountType Type, string Currency);
    }
}

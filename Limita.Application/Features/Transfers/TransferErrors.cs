using Limita.Application.Common;

namespace Limita.Application.Features.Transfers;

public static class TransferErrors
{
    public static readonly Error AuthRequired =
        Error.Unauthorized("Auth.Required", "Sign-in is required.");

    public static readonly Error AccountNotFound =
        Error.NotFound("Account.NotFound", "The account was not found.");

    public static readonly Error BankNotFound =
        Error.NotFound("Bank.NotFound", "The bank was not found.");

    public static readonly Error BranchNotFound =
        Error.NotFound("Branch.NotFound", "The branch was not found.");

    public static readonly Error BranchNotInBank =
        Error.BusinessRule("Branch.NotInBank", "The branch does not belong to the selected bank.");

    public static readonly Error BeneficiaryNotFound =
        Error.NotFound("Beneficiary.NotFound", "The beneficiary was not found.");
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limita.Domain.Enums
{
    public enum AccountType { Current, Savings }

    public enum AccountStatus { Active, Frozen, Closed }

    public enum CardNetwork { Visa, Mastercard, Meeza, Other }

    public enum CardStatus { Active, Blocked, Closed }

    public enum TransactionType { Debit, Credit }

    public enum TransactionCategory
    {
        General,
        Transfer,
        Savings,
        Shopping,
        Bills,
        Food,
        Transport,
        Entertainment,
        Health,
        Education,
        Other,
        MobileTopUp,
        Exchange
    }

    public enum TransferStatus { Pending, Completed, Failed }

    public enum LimitPeriod { Daily, Weekly, Monthly }

    public enum SavingsGoalStatus { Active, Completed, Cancelled }

    public enum NotificationType
    {
        General,
        TransferSent,
        TransferReceived,
        SpendingLimitExceeded,
        CardAdded,
        SavingsGoalReached
    }

    public enum OtpPurpose { PhoneVerification, PasswordReset }

    public enum WithdrawalStatus
    {
        PendingVerification,
        Completed,
        Expired,
        Cancelled
    }

    public enum TimeDepositStatus
    {
        Active,
        Matured,
        Redeemed
    }

}

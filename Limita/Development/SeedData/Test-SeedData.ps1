<#
.SYNOPSIS
    Validates seed-data.json against the Limita EF Core schema and business rules.
.DESCRIPTION
    Pure data validation (no database access): property allow-lists, enum values,
    GUID/FK resolution, uniqueness, max lengths, money/check constraints, ledger
    running-balance consistency and cross-record integrity.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Test-SeedData.ps1
#>
[CmdletBinding()]
param(
    [string]$File
)

if ([string]::IsNullOrEmpty($File)) { $File = Join-Path $PSScriptRoot 'seed-data.json' }

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$script:Errors = New-Object System.Collections.Generic.List[string]
function Err([string]$m) { $script:Errors.Add($m) | Out-Null }
function Check([bool]$cond, [string]$m) { if (-not $cond) { Err $m } }

if (-not (Test-Path -LiteralPath $File)) { throw "Seed file not found: $File" }
$data = Get-Content -LiteralPath $File -Raw | ConvertFrom-Json

$script:Banks = @(
    'a1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000002',
    'a1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000004',
    'a1000000-0000-0000-0000-000000000005')
$script:BankBranches = @(
    'b1000000-0000-0000-0000-000000000001', 'b1000000-0000-0000-0000-000000000002',
    'b1000000-0000-0000-0000-000000000003', 'b1000000-0000-0000-0000-000000000004',
    'b1000000-0000-0000-0000-000000000005', 'b1000000-0000-0000-0000-000000000006',
    'b1000000-0000-0000-0000-000000000007')

$ENUM = @{
    AccountType          = @('Current','Savings')
    AccountStatus        = @('Active','Frozen','Closed')
    CardNetwork          = @('Visa','Mastercard','Meeza','Other')
    CardStatus           = @('Active','Blocked','Closed')
    TransactionType      = @('Debit','Credit')
    TransactionCategory  = @('General','Transfer','Savings','Shopping','Bills','Food','Transport','Entertainment','Health','Education','Other','MobileTopUp','Exchange')
    TransferStatus       = @('Pending','Completed','Failed')
    LimitPeriod          = @('Daily','Weekly','Monthly')
    SavingsGoalStatus    = @('Active','Completed','Cancelled')
    NotificationType     = @('General','TransferSent','TransferReceived','SpendingLimitExceeded','SpendingLimitHalfwayReached','CardAdded','SavingsGoalReached','PaymentSuccessful','PayInOneTap','ShoppingLimit')
    OtpPurpose           = @('PhoneVerification','PasswordReset')
    WithdrawalStatus     = @('PendingVerification','Completed','Expired','Cancelled')
    TimeDepositStatus    = @('Active','Matured','Redeemed')
    BillType             = @('Electric','Water','Internet','Mobile')
    BillStatus           = @('Unpaid','Paid')
    TransferType         = @('CardNumber','SameBank','OtherBank')
    VerificationMethod   = @('Otp','TouchId','FaceId')
}

function Is-Guid([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $false }
    $g = [Guid]::Empty
    return [Guid]::TryParse($s, [ref]$g)
}
function Is-DateTime([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $false }
    $d = [DateTimeOffset]::MinValue
    return [DateTimeOffset]::TryParse($s, [ref]$d)
}
function Is-DateOnly([string]$s) {
    if ([string]::IsNullOrWhiteSpace($s)) { return $false }
    $d = [DateTime]::MinValue
    return [DateTime]::TryParseExact($s, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$d)
}
function To-Dto([string]$s) { return [DateTimeOffset]::Parse($s, [Globalization.CultureInfo]::InvariantCulture) }

function Assert-Money($m, [string]$where, [bool]$positive = $false) {
    if ($null -eq $m) { Err "${where}: money is null"; return }
    $names = @($m.PSObject.Properties.Name)
    if ($names -notcontains 'Amount') { Err "${where}: money missing Amount"; return }
    if ($names -notcontains 'Currency') { Err "${where}: money missing Currency"; return }
    if ($m.Amount -isnot [ValueType]) { Err "${where}: money Amount not numeric"; return }
    $d = [decimal]$m.Amount
    if ($positive -and $d -le 0) { Err "${where}: amount must be > 0 (got $d)" }
    if ($d -lt 0) { Err "${where}: amount is negative ($d)" }
    if ([decimal]::Round($d, 2) -ne $d) { Err "${where}: amount has more than 2 decimals ($d)" }
    if ($m.Currency -notmatch '^[A-Z]{3}$') { Err "${where}: bad currency '$($m.Currency)'" }
}
function Money-Same($a, $b, [string]$where) {
    if ($null -eq $a -or $null -eq $b) { return }
    if ($a.Currency -ne $b.Currency) { Err "${where}: currency mismatch ($($a.Currency) vs $($b.Currency))" }
}

function Assert-Shape($items, [string]$name, [string[]]$allowed) {
    $i = 0
    foreach ($it in $items) {
        $i++
        $props = @($it.PSObject.Properties.Name)
        foreach ($p in $props) { if ($allowed -notcontains $p) { Err "$name[$i]: unexpected property '$p'" } }
        foreach ($p in $allowed) { if ($props -notcontains $p) { Err "$name[$i]: missing property '$p'" } }
    }
}
function Assert-Max($value, [int]$max, [string]$where) {
    if ($null -eq $value) { return }
    if ($value.ToString().Length -gt $max) { Err "${where}: exceeds max length $max" }
}
function Assert-Enum($value, [string]$enumName, [string]$where) {
    if ($ENUM[$enumName] -notcontains $value) { Err "${where}: invalid $enumName '$value'" }
}
function Assert-DateTime($value, [string]$where) {
    if (-not (Is-DateTime $value)) { Err "${where}: invalid date/time '$value'" }
}
function Assert-DateOnly($value, [string]$where) {
    if (-not (Is-DateOnly $value)) { Err "${where}: invalid date '$value'" }
}

# ── SHAPES ──────────────────────────────────────────────────────────────────
Assert-Shape $data.Users 'Users' @('Id','FullName','Email','PhoneNumber','PasswordHash','IsPhoneVerified','ProfilePictureUrl','CreatedAt','PreferredLanguageCode','FailedLoginAttempts','LockedUntil')
Assert-Shape $data.Accounts 'Accounts' @('Id','UserId','AccountNumber','Type','Balance','Status')
Assert-Shape $data.Transactions 'Transactions' @('Id','AccountId','TransferId','SavingsGoalId','WithdrawalId','TimeDepositId','ExchangeOperationId','Type','Amount','BalanceAfter','Category','Description','CreatedAt')
Assert-Shape $data.Cards 'Cards' @('Id','AccountId','Network','Last4','ExpiryMonth','ExpiryYear','HolderName','Status','Token')
Assert-Shape $data.Beneficiaries 'Beneficiaries' @('Id','UserId','Name','TransferType','CardNumberLast4','CardNumberHash','AccountNumber','BankId','BranchId','CreatedAt')
Assert-Shape $data.Transfers 'Transfers' @('Id','FromAccountId','ToAccountId','Amount','Fee','Status','IdempotencyKey','Note','CreatedAt','CustomerId','TransferType','VerificationMethod','BeneficiaryId','FailureReason')
Assert-Shape $data.Bills 'Bills' @('Id','Type','Code','CustomerName','Company','PeriodFrom','PeriodTo','Fee','Tax','DueDate','Status','CustomerUserId')
Assert-Shape $data.BillPayments 'BillPayments' @('Id','UserId','BillId','AccountId','TransactionId','BillType','Amount','IdempotencyKey','CreatedAt')
Assert-Shape $data.MobileTopUps 'MobileTopUps' @('Id','UserId','AccountId','TransactionId','PhoneNumber','Amount','IdempotencyKey','CreatedAt')
Assert-Shape $data.SavingsGoals 'SavingsGoals' @('Id','UserId','AccountId','Name','TargetAmount','SavedAmount','TargetDate','Status')
Assert-Shape $data.SpendingLimits 'SpendingLimits' @('Id','UserId','CardId','AccountId','Category','Period','LimitAmount','LastHalfwayNotificationPeriodStart','LastExceededNotificationPeriodStart','IsActive')
Assert-Shape $data.TimeDeposits 'TimeDeposits' @('Id','UserId','FundingAccountId','Principal','TermInMonths','AnnualInterestRatePercent','CreatedAt','MaturesAt','Status')
Assert-Shape $data.Withdrawals 'Withdrawals' @('Id','UserId','AccountId','PhoneNumber','Amount','CodeHash','Status','FailedVerificationAttempts','CreatedAt','ExpiresAt','CompletedAt')
Assert-Shape $data.ExchangeOperations 'ExchangeOperations' @('Id','UserId','SourceAccountId','TargetAccountId','SourceAmount','TargetAmount','ExchangeRate','CreatedAt')
Assert-Shape $data.Notifications 'Notifications' @('Id','UserId','Type','Title','Body','IsRead','RelatedEntityId','CreatedAt')
Assert-Shape $data.OtpCodes 'OtpCodes' @('Id','UserId','Purpose','CodeHash','CreatedAt','ExpiresAt','ConsumedAt','FailedAttempts')
Assert-Shape $data.RefreshTokens 'RefreshTokens' @('Id','UserId','TokenHash','ExpiresAt','CreatedAt','RevokedAt')
Assert-Shape $data.Branches 'Branches' @('Id','Name','Address','City','PhoneNumber','Latitude','Longitude','IsActive')
Assert-Shape $data.InterestRates 'InterestRates' @('Id','ProductCode','ProductName','AnnualRatePercent','MinimumTermMonths','MaximumTermMonths','MinimumAmount','CurrencyCode','EffectiveFrom','IsActive')
Assert-Shape $data.ExchangeRates 'ExchangeRates' @('Id','BaseCurrency','QuoteCurrency','Rate','BuyRate','SellRate','UpdatedAt','IsActive')
Assert-Shape $data.MessageThreads 'MessageThreads' @('Id','UserId','Subject','Status','CreatedAt','UpdatedAt')
Assert-Shape $data.Messages 'Messages' @('Id','ThreadId','SenderUserId','Body','IsFromBank','SentAt')

# ── ID SETS + GLOBAL UNIQUENESS ─────────────────────────────────────────────
$ids = @{}
$sets = [ordered]@{}
foreach ($name in @('Users','Accounts','Transactions','Cards','Beneficiaries','Transfers','Bills','BillPayments','MobileTopUps','SavingsGoals','SpendingLimits','TimeDeposits','Withdrawals','ExchangeOperations','Notifications','OtpCodes','RefreshTokens','Branches','InterestRates','ExchangeRates','MessageThreads','Messages')) {
    $set = @{}
    $i = 0
    foreach ($it in $data.$name) {
        $i++
        if (-not (Is-Guid $it.Id)) { Err "$name[$i]: invalid Id '$($it.Id)'"; continue }
        if ($set.ContainsKey($it.Id)) { Err "$name[$i]: duplicate Id '$($it.Id)'" }
        else { $set[$it.Id] = $it }
        if ($ids.ContainsKey($it.Id)) { Err "$name[$i]: Id '$($it.Id)' also used by $($ids[$it.Id])" }
        else { $ids[$it.Id] = $name }
    }
    $sets[$name] = $set
}
$UserSet = $sets.Users; $AcctSet = $sets.Accounts; $TxnSet = $sets.Transactions; $CardSet = $sets.Cards
$BenSet = $sets.Beneficiaries; $TR = $sets.Transfers; $BillSet = $sets.Bills; $BP = $sets.BillPayments
$MT = $sets.MobileTopUps; $SG = $sets.SavingsGoals; $SL = $sets.SpendingLimits; $TD = $sets.TimeDeposits
$WD = $sets.Withdrawals; $EX = $sets.ExchangeOperations; $NT = $sets.Notifications; $OT = $sets.OtpCodes
$RT = $sets.RefreshTokens; $BR = $sets.Branches; $IR = $sets.InterestRates; $ER = $sets.ExchangeRates
$TH = $sets.MessageThreads; $MS = $sets.Messages

function ReqFK($set, $id, [string]$where, [string]$target) {
    if (-not (Is-Guid $id)) { Err "${where}: invalid $target reference '$id'"; return $false }
    if (-not $set.ContainsKey($id)) { Err "${where}: $target '$id' not found"; return $false }
    return $true
}
function OptFK($set, $id, [string]$where, [string]$target) {
    if ($null -eq $id) { return $true }
    return (ReqFK $set $id $where $target)
}

# ── USERS ───────────────────────────────────────────────────────────────────
$emails = @{}; $phones = @{}
$ui = 0
foreach ($u in $data.Users) {
    $ui++; $w = "Users[$ui]"
    Assert-Max $u.FullName 100 "$w.FullName"; Check ($u.FullName.Length -gt 0) "$w.FullName empty"
    Assert-Max $u.Email 256 "$w.Email"; Check ($u.Email -ceq $u.Email.ToLowerInvariant()) "$w.Email not lower-case"
    Assert-Max $u.PhoneNumber 20 "$w.PhoneNumber"
    Check ($u.PhoneNumber -match '^\+[1-9]\d{9,14}$') "$w.PhoneNumber invalid '$($u.PhoneNumber)'"
    Check ($u.PasswordHash -like 'HASH:*') "$w.PasswordHash must start with 'HASH:'"
    Assert-DateTime $u.CreatedAt "$w.CreatedAt"
    Check (($u.PreferredLanguageCode -eq 'en') -or ($u.PreferredLanguageCode -eq 'ar')) "$w.PreferredLanguageCode '$($u.PreferredLanguageCode)'"
    Check ($u.FailedLoginAttempts -ge 0 -and $u.FailedLoginAttempts -lt 5) "$w.FailedLoginAttempts out of range"
    if ($null -ne $u.LockedUntil) { Assert-DateTime $u.LockedUntil "$w.LockedUntil" }
    if ($emails.ContainsKey($u.Email)) { Err "$w.Email duplicate" } else { $emails[$u.Email] = 1 }
    if ($phones.ContainsKey($u.PhoneNumber)) { Err "$w.PhoneNumber duplicate" } else { $phones[$u.PhoneNumber] = 1 }
}

# ── ACCOUNTS ────────────────────────────────────────────────────────────────
$acctNumbers = @{}
$ai = 0
foreach ($a in $data.Accounts) {
    $ai++; $w = "Accounts[$ai]"
    ReqFK $UserSet $a.UserId "$w.UserId" 'User' | Out-Null
    Assert-Max $a.AccountNumber 34 "$w.AccountNumber"
    Assert-Enum $a.Type 'AccountType' "$w.Type"
    Assert-Enum $a.Status 'AccountStatus' "$w.Status"
    Assert-Money $a.Balance "$w.Balance"
    if ($acctNumbers.ContainsKey($a.AccountNumber)) { Err "$w.AccountNumber duplicate" } else { $acctNumbers[$a.AccountNumber] = 1 }
    if ($a.Status -eq 'Closed') { Check ([decimal]$a.Balance.Amount -eq 0) "${w}: closed account must have zero balance" }
}

# ── TRANSACTIONS (shape + FK + ledger running balance) ──────────────────────
$byAccount = @{}
$ti = 0
foreach ($t in $data.Transactions) {
    $ti++; $w = "Transactions[$ti]"
    ReqFK $AcctSet $t.AccountId "$w.AccountId" 'Account' | Out-Null
    OptFK $TR $t.TransferId "$w.TransferId" 'Transfer' | Out-Null
    OptFK $SG $t.SavingsGoalId "$w.SavingsGoalId" 'SavingsGoal' | Out-Null
    OptFK $WD $t.WithdrawalId "$w.WithdrawalId" 'Withdrawal' | Out-Null
    OptFK $TD $t.TimeDepositId "$w.TimeDepositId" 'TimeDeposit' | Out-Null
    OptFK $EX $t.ExchangeOperationId "$w.ExchangeOperationId" 'ExchangeOperation' | Out-Null
    Assert-Enum $t.Type 'TransactionType' "$w.Type"
    Assert-Money $t.Amount "$w.Amount" $true
    Assert-Enum $t.Category 'TransactionCategory' "$w.Category"
    Assert-DateTime $t.CreatedAt "$w.CreatedAt"
    Assert-Max $t.Description 200 "$w.Description"
    Check ($t.BalanceAfter -is [ValueType]) "$w.BalanceAfter not numeric"
    if (-not $byAccount.ContainsKey($t.AccountId)) { $byAccount[$t.AccountId] = New-Object System.Collections.Generic.List[object] }
    $byAccount[$t.AccountId].Add($t) | Out-Null
}
foreach ($acctId in $byAccount.Keys) {
    $rows = @($byAccount[$acctId] | Sort-Object CreatedAt, Id)
    $running = [decimal]0
    foreach ($r in $rows) {
        $amt = [decimal]$r.Amount.Amount
        if ($r.Type -eq 'Debit') { $running -= $amt } else { $running += $amt }
        if ($running -lt 0) { Err "Ledger ${acctId}: running balance went negative at txn $($r.Id)" }
        if ([decimal]::Round([decimal]$r.BalanceAfter, 2) -ne [decimal]::Round($running, 2)) {
            Err "Ledger ${acctId}: BalanceAfter $($r.BalanceAfter) != computed $running at txn $($r.Id)"
        }
        if ($AcctSet.ContainsKey($acctId)) {
            Money-Same $r.Amount $AcctSet[$acctId].Balance "Ledger $acctId txn $($r.Id)"
        }
    }
    if ($AcctSet.ContainsKey($acctId)) {
        if ([decimal]::Round($running, 2) -ne [decimal]::Round([decimal]$AcctSet[$acctId].Balance.Amount, 2)) {
            Err "Ledger ${acctId}: final computed $running != account balance $($AcctSet[$acctId].Balance.Amount)"
        }
    }
}

# ── CARDS ───────────────────────────────────────────────────────────────────
$now = [DateTimeOffset]::UtcNow
$ci = 0
foreach ($c in $data.Cards) {
    $ci++; $w = "Cards[$ci]"
    ReqFK $AcctSet $c.AccountId "$w.AccountId" 'Account' | Out-Null
    Assert-Enum $c.Network 'CardNetwork' "$w.Network"
    Assert-Enum $c.Status 'CardStatus' "$w.Status"
    Check ($c.Last4 -match '^\d{4}$') "$w.Last4 must be 4 digits"
    Check ($c.ExpiryMonth -ge 1 -and $c.ExpiryMonth -le 12) "$w.ExpiryMonth out of range"
    Check ($c.ExpiryYear -ge 2000 -and $c.ExpiryYear -le 2100) "$w.ExpiryYear out of range"
    Assert-Max $c.HolderName 100 "$w.HolderName"
    Assert-Max $c.Token 200 "$w.Token"
    if ($c.Status -eq 'Active') {
        Check (($c.ExpiryYear -gt $now.Year) -or ($c.ExpiryYear -eq $now.Year -and $c.ExpiryMonth -ge $now.Month)) "${w}: active card is expired"
    }
}

# ── BENEFICIARIES ───────────────────────────────────────────────────────────
$bi = 0
foreach ($b in $data.Beneficiaries) {
    $bi++; $w = "Beneficiaries[$bi]"
    ReqFK $UserSet $b.UserId "$w.UserId" 'User' | Out-Null
    Assert-Max $b.Name 100 "$w.Name"
    Assert-Enum $b.TransferType 'TransferType' "$w.TransferType"
    Assert-DateTime $b.CreatedAt "$w.CreatedAt"
    if ($b.TransferType -eq 'CardNumber') {
        Check ($b.CardNumberLast4 -match '^\d{4}$') "$w.CardNumberLast4 must be 4 digits"
        Check ($b.CardNumberHash -match '^SHA256:\d+$') "$w.CardNumberHash must be a 'SHA256:<card number>' sentinel"
        if ($b.CardNumberHash -match '^SHA256:(\d+)$') {
            Check ($Matches[1].Substring($Matches[1].Length - 4) -eq $b.CardNumberLast4) "${w}: hash raw card does not end with CardNumberLast4"
        }
        Check ($null -eq $b.BankId) "${w}: CardNumber beneficiary must not set BankId"
    } else {
        Assert-Max $b.AccountNumber 30 "$w.AccountNumber"
        Check (-not [string]::IsNullOrWhiteSpace($b.AccountNumber)) "$w.AccountNumber required"
        Check ($script:Banks -contains $b.BankId) "$w.BankId not a seed bank '$($b.BankId)'"
        Check ($script:BankBranches -contains $b.BranchId) "$w.BranchId not a seed branch '$($b.BranchId)'"
    }
}

# ── TRANSFERS ───────────────────────────────────────────────────────────────
$tkeys = @{}
$tri = 0
foreach ($t in $data.Transfers) {
    $tri++; $w = "Transfers[$tri]"
    ReqFK $AcctSet $t.FromAccountId "$w.FromAccountId" 'Account' | Out-Null
    if ($null -ne $t.ToAccountId) {
        ReqFK $AcctSet $t.ToAccountId "$w.ToAccountId" 'Account' | Out-Null
        Check ($t.ToAccountId -ne $t.FromAccountId) "${w}: same from/to account"
    }
    Assert-Money $t.Amount "$w.Amount" $true
    Assert-Money $t.Fee "$w.Fee"
    Money-Same $t.Amount $t.Fee "$w.Fee"
    Assert-Enum $t.Status 'TransferStatus' "$w.Status"
    Assert-Enum $t.TransferType 'TransferType' "$w.TransferType"
    Assert-Enum $t.VerificationMethod 'VerificationMethod' "$w.VerificationMethod"
    Assert-Max $t.IdempotencyKey 100 "$w.IdempotencyKey"
    Assert-Max $t.Note 200 "$w.Note"
    Assert-Max $t.FailureReason 500 "$w.FailureReason"
    Assert-DateTime $t.CreatedAt "$w.CreatedAt"
    OptFK $BenSet $t.BeneficiaryId "$w.BeneficiaryId" 'Beneficiary' | Out-Null
    OptFK $UserSet $t.CustomerId "$w.CustomerId" 'User' | Out-Null
    if ($null -ne $t.ToAccountId) { Check ([decimal]$t.Fee.Amount -eq 0) "${w}: internal transfer fee must be 0" }
    if ($t.Status -eq 'Pending' -or $t.Status -eq 'Failed') { Check ($null -eq $t.ToAccountId) "${w}: $($t.Status) transfer must be external" }
    if ($t.Status -eq 'Failed') { Check (-not [string]::IsNullOrWhiteSpace($t.FailureReason)) "${w}: failed transfer needs a FailureReason" }
    else { Check ($null -eq $t.FailureReason) "${w}: non-failed transfer must not set FailureReason" }
    $key = "$($t.FromAccountId)|$($t.IdempotencyKey)"
    if ($tkeys.ContainsKey($key)) { Err "${w}: duplicate (FromAccountId,IdempotencyKey)" } else { $tkeys[$key] = 1 }
}

# ── BILLS + BILL PAYMENTS ───────────────────────────────────────────────────
$billCodes = @{}; $billPaid = @{}
$bii = 0
foreach ($b in $data.Bills) {
    $bii++; $w = "Bills[$bii]"
    Assert-Enum $b.Type 'BillType' "$w.Type"
    Assert-Enum $b.Status 'BillStatus' "$w.Status"
    Assert-Max $b.Code 50 "$w.Code"; Assert-Max $b.CustomerName 100 "$w.CustomerName"; Assert-Max $b.Company 100 "$w.Company"
    Assert-DateOnly $b.PeriodFrom "$w.PeriodFrom"; Assert-DateOnly $b.PeriodTo "$w.PeriodTo"; Assert-DateOnly $b.DueDate "$w.DueDate"
    Check ([datetime]$b.PeriodTo -ge [datetime]$b.PeriodFrom) "${w}: PeriodTo before PeriodFrom"
    Assert-Money $b.Fee "$w.Fee" $true
    Assert-Money $b.Tax "$w.Tax"
    Money-Same $b.Fee $b.Tax "$w.Tax"
    OptFK $UserSet $b.CustomerUserId "$w.CustomerUserId" 'User' | Out-Null
    $key = "$($b.Type)|$($b.Code)"
    if ($billCodes.ContainsKey($key)) { Err "${w}: duplicate (Type,Code)" } else { $billCodes[$key] = 1 }
}
$bpKeys = @{}
$bpi = 0
foreach ($p in $data.BillPayments) {
    $bpi++; $w = "BillPayments[$bpi]"
    ReqFK $UserSet $p.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $p.AccountId "$w.AccountId" 'Account' | Out-Null
    ReqFK $BillSet $p.BillId "$w.BillId" 'Bill' | Out-Null
    ReqFK $TxnSet $p.TransactionId "$w.TransactionId" 'Transaction' | Out-Null
    Assert-Enum $p.BillType 'BillType' "$w.BillType"
    Assert-Money $p.Amount "$w.Amount" $true
    Assert-Max $p.IdempotencyKey 100 "$w.IdempotencyKey"
    Assert-DateTime $p.CreatedAt "$w.CreatedAt"
    if ($BillSet.ContainsKey($p.BillId)) {
        $bill = $BillSet[$p.BillId]
        Check ($bill.Status -eq 'Paid') "${w}: bill is not Paid"
        $billPaid[$p.BillId] = 1
        $expected = [decimal]$bill.Fee.Amount + [decimal]$bill.Tax.Amount
        Check ([decimal]::Round([decimal]$p.Amount.Amount,2) -eq [decimal]::Round($expected,2)) "${w}: amount != bill Fee+Tax ($($p.Amount.Amount) vs $expected)"
        Money-Same $p.Amount $bill.Fee "$w.Amount"
        Check ($p.BillType -eq $bill.Type) "${w}: BillType != bill.Type"
    }
    if ($TxnSet.ContainsKey($p.TransactionId)) {
        $txn = $TxnSet[$p.TransactionId]
        Check ($txn.AccountId -eq $p.AccountId) "${w}: txn.AccountId != payment AccountId"
        Check ($txn.Type -eq 'Debit') "${w}: payment txn must be a Debit"
        Check ($txn.Category -eq 'Bills') "${w}: payment txn category must be Bills"
        Money-Same $txn.Amount $p.Amount "${w}: txn amount"
    }
    $key = "$($p.UserId)|$($p.IdempotencyKey)"
    if ($bpKeys.ContainsKey($key)) { Err "${w}: duplicate (UserId,IdempotencyKey)" } else { $bpKeys[$key] = 1 }
}

# ── MOBILE TOP-UPS ──────────────────────────────────────────────────────────
$mtKeys = @{}
$mi = 0
foreach ($m in $data.MobileTopUps) {
    $mi++; $w = "MobileTopUps[$mi]"
    ReqFK $UserSet $m.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $m.AccountId "$w.AccountId" 'Account' | Out-Null
    ReqFK $TxnSet $m.TransactionId "$w.TransactionId" 'Transaction' | Out-Null
    Check ($m.PhoneNumber -match '^\+?\d{8,15}$') "$w.PhoneNumber invalid"
    Assert-Money $m.Amount "$w.Amount" $true
    Assert-Max $m.IdempotencyKey 100 "$w.IdempotencyKey"
    Assert-DateTime $m.CreatedAt "$w.CreatedAt"
    if ($TxnSet.ContainsKey($m.TransactionId)) {
        $txn = $TxnSet[$m.TransactionId]
        Check ($txn.AccountId -eq $m.AccountId) "${w}: txn.AccountId != topup AccountId"
        Check ($txn.Type -eq 'Debit') "${w}: topup txn must be a Debit"
        Check ($txn.Category -eq 'MobileTopUp') "${w}: topup txn category must be MobileTopUp"
        Money-Same $txn.Amount $m.Amount "${w}: txn amount"
    }
    $key = "$($m.UserId)|$($m.IdempotencyKey)"
    if ($mtKeys.ContainsKey($key)) { Err "${w}: duplicate (UserId,IdempotencyKey)" } else { $mtKeys[$key] = 1 }
}

# ── SAVINGS GOALS ───────────────────────────────────────────────────────────
$goalContrib = @{}
foreach ($t in $data.Transactions) {
    if ($null -ne $t.SavingsGoalId -and $t.Type -eq 'Debit') {
        if (-not $goalContrib.ContainsKey($t.SavingsGoalId)) { $goalContrib[$t.SavingsGoalId] = [decimal]0 }
        $goalContrib[$t.SavingsGoalId] += [decimal]$t.Amount.Amount
    }
}
$gi = 0
foreach ($g in $data.SavingsGoals) {
    $gi++; $w = "SavingsGoals[$gi]"
    ReqFK $UserSet $g.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $g.AccountId "$w.AccountId" 'Account' | Out-Null
    Assert-Max $g.Name 100 "$w.Name"
    Assert-Enum $g.Status 'SavingsGoalStatus' "$w.Status"
    Assert-Money $g.TargetAmount "$w.TargetAmount" $true
    Assert-Money $g.SavedAmount "$w.SavedAmount"
    Money-Same $g.TargetAmount $g.SavedAmount "$w.SavedAmount"
    if ($null -ne $g.TargetDate) { Assert-DateOnly $g.TargetDate "$w.TargetDate" }
    Check ([decimal]$g.SavedAmount.Amount -le [decimal]$g.TargetAmount.Amount) "${w}: SavedAmount exceeds TargetAmount"
    if ($g.Status -eq 'Completed') { Check ([decimal]$g.SavedAmount.Amount -eq [decimal]$g.TargetAmount.Amount) "${w}: completed goal not fully saved" }
    if ($goalContrib.ContainsKey($g.Id)) {
        Check ([decimal]::Round($goalContrib[$g.Id],2) -eq [decimal]::Round([decimal]$g.SavedAmount.Amount,2)) "${w}: contributions $($goalContrib[$g.Id]) != SavedAmount $($g.SavedAmount.Amount)"
    }
}

# ── SPENDING LIMITS ─────────────────────────────────────────────────────────
$sli = 0
foreach ($s in $data.SpendingLimits) {
    $sli++; $w = "SpendingLimits[$sli]"
    ReqFK $UserSet $s.UserId "$w.UserId" 'User' | Out-Null
    $hasCard = $null -ne $s.CardId
    $hasAcct = $null -ne $s.AccountId
    Check ($hasCard -xor $hasAcct) "${w}: exactly one of CardId/AccountId must be set"
    if ($hasCard) { ReqFK $CardSet $s.CardId "$w.CardId" 'Card' | Out-Null }
    if ($hasAcct) { ReqFK $AcctSet $s.AccountId "$w.AccountId" 'Account' | Out-Null }
    if ($null -ne $s.Category) { Assert-Enum $s.Category 'TransactionCategory' "$w.Category" }
    Assert-Enum $s.Period 'LimitPeriod' "$w.Period"
    Assert-Money $s.LimitAmount "$w.LimitAmount" $true
    if ($null -ne $s.LastHalfwayNotificationPeriodStart) { Assert-DateTime $s.LastHalfwayNotificationPeriodStart "$w.LastHalfway" }
    if ($null -ne $s.LastExceededNotificationPeriodStart) { Assert-DateTime $s.LastExceededNotificationPeriodStart "$w.LastExceeded" }
    Check ($s.IsActive -is [bool]) "$w.IsActive not boolean"
}

# ── TIME DEPOSITS ───────────────────────────────────────────────────────────
$tdi = 0
foreach ($d in $data.TimeDeposits) {
    $tdi++; $w = "TimeDeposits[$tdi]"
    ReqFK $UserSet $d.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $d.FundingAccountId "$w.FundingAccountId" 'Account' | Out-Null
    Assert-Money $d.Principal "$w.Principal" $true
    Check ([decimal]$d.Principal.Amount -ge 1000) "${w}: principal below 1000"
    Check (@(3,6,12,16,24) -contains [int]$d.TermInMonths) "${w}: unsupported term $($d.TermInMonths)"
    Check ([decimal]$d.AnnualInterestRatePercent -gt 0 -and [decimal]$d.AnnualInterestRatePercent -le 100) "${w}: rate out of range"
    Assert-Enum $d.Status 'TimeDepositStatus' "$w.Status"
    Assert-DateTime $d.CreatedAt "$w.CreatedAt"
    Assert-DateTime $d.MaturesAt "$w.MaturesAt"
    $exp = (To-Dto $d.CreatedAt).AddMonths([int]$d.TermInMonths)
    Check ((To-Dto $d.MaturesAt) -eq $exp) "${w}: MaturesAt != CreatedAt+$($d.TermInMonths) months"
    if ($d.Status -eq 'Redeemed') { Check ((To-Dto $d.MaturesAt) -le $now) "${w}: Redeemed before maturity" }
}

# ── WITHDRAWALS ─────────────────────────────────────────────────────────────
$wdTxn = @{}
foreach ($t in $data.Transactions) { if ($null -ne $t.WithdrawalId) { $wdTxn[$t.WithdrawalId] = 1 } }
$wdi = 0
foreach ($x in $data.Withdrawals) {
    $wdi++; $w = "Withdrawals[$wdi]"
    ReqFK $UserSet $x.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $x.AccountId "$w.AccountId" 'Account' | Out-Null
    Assert-Max $x.PhoneNumber 20 "$w.PhoneNumber"
    Assert-Money $x.Amount "$w.Amount" $true
    Check ($x.CodeHash -like 'HMAC:*') "$w.CodeHash must start with 'HMAC:'"
    Assert-Enum $x.Status 'WithdrawalStatus' "$w.Status"
    Check ($x.FailedVerificationAttempts -ge 0 -and $x.FailedVerificationAttempts -le 3) "$w.FailedVerificationAttempts out of range"
    Assert-DateTime $x.CreatedAt "$w.CreatedAt"; Assert-DateTime $x.ExpiresAt "$w.ExpiresAt"
    Check ((To-Dto $x.ExpiresAt) -eq (To-Dto $x.CreatedAt).AddMinutes(5)) "${w}: ExpiresAt != CreatedAt+5min"
    if ($x.Status -eq 'Completed') {
        Check ($null -ne $x.CompletedAt) "${w}: completed withdrawal needs CompletedAt"
        Check $wdTxn.ContainsKey($x.Id) "${w}: completed withdrawal has no ledger entry"
    } else {
        Check ($null -eq $x.CompletedAt) "${w}: non-completed withdrawal must not set CompletedAt"
        Check (-not $wdTxn.ContainsKey($x.Id)) "${w}: non-completed withdrawal must not have a ledger entry"
    }
}

# ── EXCHANGE OPERATIONS + legs ──────────────────────────────────────────────
$exTxn = @{}
foreach ($t in $data.Transactions) {
    if ($null -ne $t.ExchangeOperationId) {
        if (-not $exTxn.ContainsKey($t.ExchangeOperationId)) { $exTxn[$t.ExchangeOperationId] = @() }
        $exTxn[$t.ExchangeOperationId] = @($exTxn[$t.ExchangeOperationId]) + $t
    }
}
$exi = 0
foreach ($e in $data.ExchangeOperations) {
    $exi++; $w = "ExchangeOperations[$exi]"
    ReqFK $UserSet $e.UserId "$w.UserId" 'User' | Out-Null
    ReqFK $AcctSet $e.SourceAccountId "$w.SourceAccountId" 'Account' | Out-Null
    ReqFK $AcctSet $e.TargetAccountId "$w.TargetAccountId" 'Account' | Out-Null
    Check ($e.SourceAccountId -ne $e.TargetAccountId) "${w}: same source/target account"
    Assert-Money $e.SourceAmount "$w.SourceAmount" $true
    Assert-Money $e.TargetAmount "$w.TargetAmount" $true
    Check ($e.SourceAmount.Currency -ne $e.TargetAmount.Currency) "${w}: source/target currencies must differ"
    Check ([decimal]$e.ExchangeRate -gt 0) "${w}: exchange rate must be positive"
    Assert-DateTime $e.CreatedAt "$w.CreatedAt"
    $legs = if ($exTxn.ContainsKey($e.Id)) { @($exTxn[$e.Id]) } else { @() }
    Check ($legs.Count -eq 2) "${w}: expected 2 ledger legs, found $($legs.Count)"
}

# ── NOTIFICATIONS ───────────────────────────────────────────────────────────
$ni = 0
foreach ($n in $data.Notifications) {
    $ni++; $w = "Notifications[$ni]"
    ReqFK $UserSet $n.UserId "$w.UserId" 'User' | Out-Null
    Assert-Enum $n.Type 'NotificationType' "$w.Type"
    Assert-Max $n.Title 100 "$w.Title"; Assert-Max $n.Body 500 "$w.Body"
    Check (-not [string]::IsNullOrWhiteSpace($n.Title)) "$w.Title empty"
    Check (-not [string]::IsNullOrWhiteSpace($n.Body)) "$w.Body empty"
    Check ($n.IsRead -is [bool]) "$w.IsRead not boolean"
    if ($null -ne $n.RelatedEntityId) { Check (Is-Guid $n.RelatedEntityId) "$w.RelatedEntityId invalid" }
    Assert-DateTime $n.CreatedAt "$w.CreatedAt"
}

# ── OTP CODES ───────────────────────────────────────────────────────────────
$oi = 0
foreach ($o in $data.OtpCodes) {
    $oi++; $w = "OtpCodes[$oi]"
    ReqFK $UserSet $o.UserId "$w.UserId" 'User' | Out-Null
    Assert-Enum $o.Purpose 'OtpPurpose' "$w.Purpose"
    Check ($o.CodeHash -like 'HMAC:*') "$w.CodeHash must start with 'HMAC:'"
    Assert-DateTime $o.CreatedAt "$w.CreatedAt"; Assert-DateTime $o.ExpiresAt "$w.ExpiresAt"
    Check ((To-Dto $o.ExpiresAt) -gt (To-Dto $o.CreatedAt)) "${w}: ExpiresAt not after CreatedAt"
    Check ($o.FailedAttempts -ge 0 -and $o.FailedAttempts -le 5) "$w.FailedAttempts out of range"
    if ($null -ne $o.ConsumedAt) { Assert-DateTime $o.ConsumedAt "$w.ConsumedAt" }
}

# ── REFRESH TOKENS ──────────────────────────────────────────────────────────
$tokenHashes = @{}
$ri = 0
foreach ($r in $data.RefreshTokens) {
    $ri++; $w = "RefreshTokens[$ri]"
    ReqFK $UserSet $r.UserId "$w.UserId" 'User' | Out-Null
    Check ($r.TokenHash -like 'HMAC:*') "$w.TokenHash must start with 'HMAC:'"
    Assert-Max $r.TokenHash 128 "$w.TokenHash"
    Assert-DateTime $r.ExpiresAt "$w.ExpiresAt"; Assert-DateTime $r.CreatedAt "$w.CreatedAt"
    Check ((To-Dto $r.ExpiresAt) -gt (To-Dto $r.CreatedAt)) "${w}: ExpiresAt not after CreatedAt"
    if ($null -ne $r.RevokedAt) { Assert-DateTime $r.RevokedAt "$w.RevokedAt" }
    if ($tokenHashes.ContainsKey($r.TokenHash)) { Err "${w}: duplicate TokenHash" } else { $tokenHashes[$r.TokenHash] = 1 }
}

# ── BRANCHES ────────────────────────────────────────────────────────────────
$bri = 0
foreach ($b in $data.Branches) {
    $bri++; $w = "Branches[$bri]"
    Assert-Max $b.Name 100 "$w.Name"; Assert-Max $b.Address 250 "$w.Address"; Assert-Max $b.City 100 "$w.City"; Assert-Max $b.PhoneNumber 20 "$w.PhoneNumber"
    Check (-not [string]::IsNullOrWhiteSpace($b.Name)) "$w.Name empty"
    Check ([decimal]$b.Latitude -ge -90 -and [decimal]$b.Latitude -le 90) "$w.Latitude out of range"
    Check ([decimal]$b.Longitude -ge -180 -and [decimal]$b.Longitude -le 180) "$w.Longitude out of range"
    Check ($b.IsActive -is [bool]) "$w.IsActive not boolean"
}

# ── INTEREST RATES ──────────────────────────────────────────────────────────
$prodCodes = @{}
$iri = 0
foreach ($x in $data.InterestRates) {
    $iri++; $w = "InterestRates[$iri]"
    Assert-Max $x.ProductCode 30 "$w.ProductCode"; Assert-Max $x.ProductName 100 "$w.ProductName"
    Check ([decimal]$x.AnnualRatePercent -ge 0 -and [decimal]$x.AnnualRatePercent -le 100) "$w.AnnualRatePercent out of range"
    Check ($x.CurrencyCode -match '^[A-Z]{3}$') "$w.CurrencyCode invalid"
    Assert-DateTime $x.EffectiveFrom "$w.EffectiveFrom"
    if ($prodCodes.ContainsKey($x.ProductCode)) { Err "${w}: duplicate ProductCode" } else { $prodCodes[$x.ProductCode] = 1 }
}

# ── EXCHANGE RATES ──────────────────────────────────────────────────────────
$eri = 0
foreach ($x in $data.ExchangeRates) {
    $eri++; $w = "ExchangeRates[$eri]"
    Check ($x.BaseCurrency -match '^[A-Z]{3}$') "$w.BaseCurrency invalid"
    Check ($x.QuoteCurrency -match '^[A-Z]{3}$') "$w.QuoteCurrency invalid"
    Check ($x.BaseCurrency -ne $x.QuoteCurrency) "${w}: base == quote"
    Check ([decimal]$x.Rate -gt 0) "$w.Rate must be positive"
    if ($null -ne $x.BuyRate) { Check ([decimal]$x.BuyRate -gt 0) "$w.BuyRate must be positive" }
    if ($null -ne $x.SellRate) { Check ([decimal]$x.SellRate -gt 0) "$w.SellRate must be positive" }
    Assert-DateTime $x.UpdatedAt "$w.UpdatedAt"
    Check ($x.IsActive -is [bool]) "$w.IsActive not boolean"
}

# ── MESSAGE THREADS + MESSAGES ──────────────────────────────────────────────
$thi = 0
foreach ($t in $data.MessageThreads) {
    $thi++; $w = "MessageThreads[$thi]"
    ReqFK $UserSet $t.UserId "$w.UserId" 'User' | Out-Null
    Assert-Max $t.Subject 120 "$w.Subject"
    Check ($t.Status -eq 'Open' -or $t.Status -eq 'Closed') "$w.Status invalid '$($t.Status)'"
    Assert-DateTime $t.CreatedAt "$w.CreatedAt"; Assert-DateTime $t.UpdatedAt "$w.UpdatedAt"
    Check ((To-Dto $t.UpdatedAt) -ge (To-Dto $t.CreatedAt)) "${w}: UpdatedAt before CreatedAt"
}
$msi = 0
foreach ($m in $data.Messages) {
    $msi++; $w = "Messages[$msi]"
    ReqFK $TH $m.ThreadId "$w.ThreadId" 'MessageThread' | Out-Null
    ReqFK $UserSet $m.SenderUserId "$w.SenderUserId" 'User' | Out-Null
    Assert-Max $m.Body 2000 "$w.Body"
    Check (-not [string]::IsNullOrWhiteSpace($m.Body)) "$w.Body empty"
    Check ($m.IsFromBank -is [bool]) "$w.IsFromBank not boolean"
    Assert-DateTime $m.SentAt "$w.SentAt"
    if ($m.IsFromBank -and $TH.ContainsKey($m.ThreadId)) {
        Check ($m.SenderUserId -eq $TH[$m.ThreadId].UserId) "${w}: bank message sender must equal thread owner"
    }
}

# ── SUMMARY ─────────────────────────────────────────────────────────────────
if ($script:Errors.Count -eq 0) {
    Write-Host "PASS - seed-data.json is valid." -ForegroundColor Green
    exit 0
}
Write-Host ("FAIL - {0} problem(s):" -f $script:Errors.Count) -ForegroundColor Red
$script:Errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
exit 1

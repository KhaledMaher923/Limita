<#
.SYNOPSIS
    Generates seed-data.json with fictional dev data for the Limita API.
.DESCRIPTION
    Entity shapes, enum values and cross-references verified against the EF Core
    model snapshot in Limita.Infrastructure\Persistence\Migrations.
    Banks (a1000000-...0001..0005), BankBranches (b1000000-...0001..0007) and
    Languages are seeded by migrations and are NOT part of this file.
    Money is emitted as { "Amount": ..., "Currency": "USD" }.
    Sentinel strings resolved by SeedDataFileSeeder at load time:
      HASH:<password>  -> IPasswordHasher.Hash
      HMAC:<raw>       -> ISecretHasher.Hash (OTP codes, withdrawal codes,
                          refresh tokens, card-number hashes)
.NOTES
    Run:  powershell -NoProfile -ExecutionPolicy Bypass -File New-SeedData.ps1
#>
param(
    [string]$OutFile = (Join-Path $PSScriptRoot 'seed-data.json')
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$GEN = [DateTimeOffset]::UtcNow

function Fmt([DateTimeOffset]$d) { $d.ToString('yyyy-MM-ddTHH:mm:ssK') }
function Day([int]$days) { $GEN.AddDays($days).ToString('yyyy-MM-dd') }
function At([int]$daysAgo, [int]$plusMinutes = 0, [int]$plusHours = 0) {
    $GEN.AddDays(-$daysAgo).AddHours($plusHours).AddMinutes($plusMinutes) }
function Gid([int]$ns, [int]$n) { '{0:d8}-0000-4000-8000-{1:d12}' -f $ns, $n }
function Mo([double]$a, [string]$c) {
    $d = [decimal]$a
    if ([decimal]::Round($d, 2) -ne $d) { throw "Money $a has more than 2 decimals" }
    [ordered]@{ Amount = $d; Currency = $c } }

# GUID namespaces: 1 user 2 account 3 txn 4 card 5 beneficiary 6 transfer 7 bill
# 8 billpayment 9 topup 10 goal 11 limit 12 deposit 13 withdrawal 14 exchangeop
# 15 notification 16 otp 17 refreshtoken 18 branch 19 interestrate 20 exchangerate
# 21 thread 22 message

$BankCBE      = 'a1000000-0000-0000-0000-000000000001'
$BankAwash    = 'a1000000-0000-0000-0000-000000000002'
$BankDashen   = 'a1000000-0000-0000-0000-000000000004'
$BankTelebirr = 'a1000000-0000-0000-0000-000000000005'
$BrCBE1       = 'b1000000-0000-0000-0000-000000000001'
$BrAwash4     = 'b1000000-0000-0000-0000-000000000004'
$BrDashen6    = 'b1000000-0000-0000-0000-000000000006'
$BrTelebirr7  = 'b1000000-0000-0000-0000-000000000007'

function Op([string]$Type, [double]$Amount, [string]$Category, [string]$Description, [DateTimeOffset]$At,
            [string]$TransferId = $null, [string]$SavingsGoalId = $null, [string]$WithdrawalId = $null,
            [string]$TimeDepositId = $null, [string]$ExchangeOperationId = $null) {
    [pscustomobject]@{
        Type = $Type; Amount = $Amount; Category = $Category; Description = $Description; At = $At
        TransferId = $TransferId; SavingsGoalId = $SavingsGoalId; WithdrawalId = $WithdrawalId
        TimeDepositId = $TimeDepositId; ExchangeOperationId = $ExchangeOperationId
    }
}

$script:TxList   = @()
$script:AcctList = @()
$script:TxSeq    = 0

function Nz([string]$v) { if ([string]::IsNullOrEmpty($v)) { $null } else { $v } }

function Add-Account {
    param([int]$N, [string]$UserId, [string]$Type, [string]$Status, [string]$Cur, [object[]]$Ops = @())
    $id = Gid 2 $N
    $bal = [decimal]0
    foreach ($op in $Ops) {
        $amt = [decimal]$op.Amount
        if ($op.Type -eq 'Debit') { $bal -= $amt } else { $bal += $amt }
        if ($bal -lt 0) { throw "Account $id went negative: $($op.Description)" }
        $script:TxSeq++
        $script:TxList += [pscustomobject][ordered]@{
            Id = Gid 3 $script:TxSeq; AccountId = $id
            TransferId = (Nz $op.TransferId); SavingsGoalId = (Nz $op.SavingsGoalId); WithdrawalId = (Nz $op.WithdrawalId)
            TimeDepositId = (Nz $op.TimeDepositId); ExchangeOperationId = (Nz $op.ExchangeOperationId)
            Type = $op.Type; Amount = (Mo $op.Amount $Cur); BalanceAfter = $bal
            Category = $op.Category; Description = $op.Description; CreatedAt = (Fmt $op.At)
        }
    }
    $script:AcctList += [pscustomobject][ordered]@{
        Id = $id; UserId = $UserId; AccountNumber = ('2610{0:D8}' -f $N)
        Type = $Type; Balance = (Mo $bal $Cur); Status = $Status
    }
    $id
}

function FindTxn([string]$acctId, [double]$amt, [string]$desc) {
    $hit = @($script:TxList | Where-Object {
        $_.AccountId -eq $acctId -and [decimal]$_.Amount.Amount -eq [decimal]$amt -and $_.Description -eq $desc })
    if ($hit.Count -ne 1) { throw "Expected 1 txn ($acctId, $amt, '$desc'), found $($hit.Count)" }
    $hit[0]
}

# ── 1. USERS ────────────────────────────────────────────────────────────────
$users = @(
    [ordered]@{ Id = (Gid 1 1); FullName = 'Mika Tesfaye';  Email = 'mika.tesfaye@limita.test';  PhoneNumber = '+251911000001'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $true;  ProfilePictureUrl = $null; CreatedAt = (Fmt (At 200)); PreferredLanguageCode = 'en'; FailedLoginAttempts = 0; LockedUntil = $null }
    [ordered]@{ Id = (Gid 1 2); FullName = 'Rania Adel';    Email = 'rania.adel@limita.test';    PhoneNumber = '+201002200002'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $true;  ProfilePictureUrl = $null; CreatedAt = (Fmt (At 195)); PreferredLanguageCode = 'en'; FailedLoginAttempts = 0; LockedUntil = $null }
    [ordered]@{ Id = (Gid 1 3); FullName = 'Daniel Okoro';  Email = 'daniel.okoro@limita.test';  PhoneNumber = '+234803300003'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $true;  ProfilePictureUrl = $null; CreatedAt = (Fmt (At 190)); PreferredLanguageCode = 'en'; FailedLoginAttempts = 0; LockedUntil = $null }
    [ordered]@{ Id = (Gid 1 4); FullName = 'Sara Haddad';   Email = 'sara.haddad@limita.test';   PhoneNumber = '+966504400004'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $false; ProfilePictureUrl = $null; CreatedAt = (Fmt (At 185)); PreferredLanguageCode = 'en'; FailedLoginAttempts = 0; LockedUntil = $null }
    [ordered]@{ Id = (Gid 1 5); FullName = 'Omar Farouk';   Email = 'omar.farouk@limita.test';   PhoneNumber = '+251911000005'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $true;  ProfilePictureUrl = $null; CreatedAt = (Fmt (At 180)); PreferredLanguageCode = 'en'; FailedLoginAttempts = 0; LockedUntil = (Fmt $GEN.AddMinutes(15)) }
    [ordered]@{ Id = (Gid 1 6); FullName = 'Layla Mansour'; Email = 'layla.mansour@limita.test'; PhoneNumber = '+201156600006'; PasswordHash = 'HASH:DevSeed#2026'; IsPhoneVerified = $true;  ProfilePictureUrl = $null; CreatedAt = (Fmt (At 175)); PreferredLanguageCode = 'ar'; FailedLoginAttempts = 2; LockedUntil = $null }
)

# ── 2. ACCOUNTS + LEDGER ────────────────────────────────────────────────────
$u1 = Gid 1 1; $u2 = Gid 1 2; $u3 = Gid 1 3; $u4 = Gid 1 4; $u5 = Gid 1 5; $u6 = Gid 1 6

$a1 = Add-Account -N 1 -UserId $u1 -Type 'Current' -Status 'Active' -Cur 'USD' -Ops @(
    (Op 'Credit' 8500 'General'   'Opening deposit'                (At 190))
    (Op 'Debit'  3000 'Savings'   'Time deposit (6 months)'        (At 185) -TimeDepositId (Gid 12 3))
    (Op 'Debit' 89.99 'Shopping'  'Weekly grocery shopping'        (At 150))
    (Op 'Debit'    45 'Food'      'Dinner with friends'            (At 140))
    (Op 'Debit'  1200 'Other'     'Apartment rent'                 (At 120))
    (Op 'Debit'    60 'Transport' 'Metro card top-up'              (At 100))
    (Op 'Debit'   300 'Savings'   'Savings goal: Emergency fund'   (At  90) -SavingsGoalId (Gid 10 1))
    (Op 'Debit'   120 'Transfer'  'Gadget purchase'                (At  70) -TransferId (Gid 6 4))
    (Op 'Debit'    10 'Transfer'  'Transfer fee'                   (At  70) -TransferId (Gid 6 4))
    (Op 'Credit'  120 'Transfer'  'Transfer refund'                (At  70) -TransferId (Gid 6 4))
    (Op 'Credit'   10 'Transfer'  'Transfer fee refund'            (At  70) -TransferId (Gid 6 4))
    (Op 'Debit'   300 'Savings'   'Savings goal: Emergency fund'   (At  60) -SavingsGoalId (Gid 10 1))
    (Op 'Debit'   300 'Savings'   'Savings goal: Emergency fund'   (At  30) -SavingsGoalId (Gid 10 1))
    (Op 'Debit'   200 'Transfer'  'Dinner split'                   (At  20) -TransferId (Gid 6 7))
    (Op 'Debit'   100 'Education' 'Online course'                  (At  15))
    (Op 'Credit'  150 'Transfer'  'Shared utilities'               (At  12) -TransferId (Gid 6 8))
    (Op 'Debit'    60 'Other'     'Cash withdrawal'                (At  10) -WithdrawalId (Gid 13 2))
    (Op 'Debit'   480 'Bills'     'Electric bill ELEC-8801'        (At   6))
    (Op 'Debit'   250 'Transfer'  'Rent share'                     (At   5) -TransferId (Gid 6 1))
    (Op 'Debit'    10 'Transfer'  'Transfer fee'                   (At   5) -TransferId (Gid 6 1))
    (Op 'Debit'    20 'MobileTopUp' 'Mobile top-up'                (At   3))
    (Op 'Credit' 2500 'General'   'Freelance payment'              (At   1))
    (Op 'Debit'   150 'Exchange'  'Exchange to EGP'                (At   1) -ExchangeOperationId (Gid 14 1))
)

$a2 = Add-Account -N 2 -UserId $u1 -Type 'Savings' -Status 'Active' -Cur 'EGP' -Ops @(
    (Op 'Credit' 12000 'General'  'Opening deposit'   (At 190))
    (Op 'Credit'  7500 'Exchange' 'Exchange from USD' (At   1) -ExchangeOperationId (Gid 14 1))
)

$a3 = Add-Account -N 3 -UserId $u2 -Type 'Current' -Status 'Active' -Cur 'EGP' -Ops @(
    (Op 'Credit' 25000 'General'   'Opening deposit'              (At 180))
    (Op 'Debit'    320 'Food'      'Family dinner'                (At 160))
    (Op 'Debit'    150 'Transport' 'Taxi rides'                   (At 140))
    (Op 'Debit'  10000 'Savings'   'Savings goal: Umrah trip'     (At 110) -SavingsGoalId (Gid 10 3))
    (Op 'Debit'  10000 'Savings'   'Savings goal: Umrah trip'     (At  80) -SavingsGoalId (Gid 10 3))
    (Op 'Debit'   1500 'Transfer'  'Monthly support'              (At  55) -TransferId (Gid 6 2))
    (Op 'Debit'     10 'Transfer'  'Transfer fee'                 (At  55) -TransferId (Gid 6 2))
    (Op 'Debit'    900 'Bills'     'Water bill WTR-5502'          (At  45))
    (Op 'Debit'    750 'Education' 'Kids school supplies'         (At  40))
    (Op 'Credit' 12000 'General'   'Monthly salary'               (At  15))
    (Op 'Credit' 10000 'Exchange'  'Exchange from USD'            (At  12) -ExchangeOperationId (Gid 14 3))
    (Op 'Debit'     50 'MobileTopUp' 'Mobile top-up'              (At   8))
    (Op 'Debit'   1500 'Savings'   'Time deposit (3 months)'      (At   3) -TimeDepositId (Gid 12 4))
)

$a4 = Add-Account -N 4 -UserId $u2 -Type 'Current' -Status 'Active' -Cur 'USD' -Ops @(
    (Op 'Credit' 3000 'General'      'Opening deposit' (At 170))
    (Op 'Debit'    65 'Entertainment' 'Concert tickets' (At 160))
    (Op 'Debit'   200 'Exchange'     'Exchange to EGP' (At  12) -ExchangeOperationId (Gid 14 3))
)

$a5 = Add-Account -N 5 -UserId $u3 -Type 'Current' -Status 'Active' -Cur 'USD' -Ops @(
    (Op 'Credit' 12500 'General'   'Opening deposit'                 (At 190))
    (Op 'Debit'   2000 'Savings'   'Time deposit (3 months)'         (At 185) -TimeDepositId (Gid 12 2))
    (Op 'Debit'     55 'Food'      'Lunch'                           (At 170))
    (Op 'Debit'    240 'Shopping'  'New sneakers'                    (At 160))
    (Op 'Debit'   5000 'Savings'   'Time deposit (12 months)'        (At 120) -TimeDepositId (Gid 12 1))
    (Op 'Credit'   100 'Exchange'  'Exchange from EGP'               (At  95) -ExchangeOperationId (Gid 14 2))
    (Op 'Debit'    400 'Transfer'  'Rent to landlord'                (At  90) -TransferId (Gid 6 5))
    (Op 'Debit'     10 'Transfer'  'Transfer fee'                    (At  90) -TransferId (Gid 6 5))
    (Op 'Credit'   400 'Transfer'  'Transfer refund'                 (At  90) -TransferId (Gid 6 5))
    (Op 'Credit'    10 'Transfer'  'Transfer fee refund'             (At  90) -TransferId (Gid 6 5))
    (Op 'Debit'     60 'Bills'     'Internet bill INT-7734'          (At  45))
    (Op 'Credit'  2020 'Savings'   'Matured time deposit (3 months)' (At  40) -TimeDepositId (Gid 12 2))
    (Op 'Debit'     40 'Health'    'Pharmacy'                        (At  30))
    (Op 'Credit'   200 'Transfer'  'Dinner split'                    (At  20) -TransferId (Gid 6 7))
    (Op 'Debit'    150 'Transfer'  'Shared utilities'                (At  12) -TransferId (Gid 6 8))
    (Op 'Debit'     75 'Transfer'  'Subscription'                    (At  11) -TransferId (Gid 6 3))
    (Op 'Debit'     10 'Transfer'  'Transfer fee'                    (At  11) -TransferId (Gid 6 3))
    (Op 'Debit'     25 'MobileTopUp' 'Mobile top-up'                 (At   4))
)

$a6 = Add-Account -N 6 -UserId $u3 -Type 'Savings' -Status 'Active' -Cur 'EGP' -Ops @(
    (Op 'Credit' 20000 'General'  'Opening deposit'  (At 170))
    (Op 'Debit'   5000 'Exchange' 'Exchange to USD'  (At  95) -ExchangeOperationId (Gid 14 2))
)

$a7 = Add-Account -N 7 -UserId $u5 -Type 'Current' -Status 'Frozen' -Cur 'EGP' -Ops @(
    (Op 'Credit' 9000 'General' 'Opening deposit' (At 150))
    (Op 'Debit'   700 'Food'    'Groceries'       (At 100))
)

$a8 = Add-Account -N 8 -UserId $u6 -Type 'Current' -Status 'Closed' -Cur 'USD' -Ops @(
    (Op 'Credit' 1200 'General'   'Opening deposit'        (At 195))
    (Op 'Debit'   600 'Shopping'  'Laptop bag'             (At 180))
    (Op 'Debit'   600 'Education' 'Certification exam fee' (At 175))
)

$a9 = Add-Account -N 9 -UserId $u6 -Type 'Savings' -Status 'Active' -Cur 'EUR' -Ops @(
    (Op 'Credit' 4000 'General'   'Opening deposit' (At 160))
    (Op 'Debit'   300 'Transfer'  'Family support'  (At  30) -TransferId (Gid 6 6))
    (Op 'Debit'    10 'Transfer'  'Transfer fee'    (At  30) -TransferId (Gid 6 6))
)

$a10 = Add-Account -N 10 -UserId $u3 -Type 'Savings' -Status 'Active' -Cur 'USD' -Ops @(
    (Op 'Credit' 1500 'General' 'Opening deposit'          (At 140))
    (Op 'Debit'   200 'Savings' 'Savings goal: New laptop' (At 100) -SavingsGoalId (Gid 10 2))
    (Op 'Debit'   200 'Savings' 'Savings goal: New laptop' (At  70) -SavingsGoalId (Gid 10 2))
)

$transactions = $script:TxList
$accounts     = $script:AcctList

# ── 3. CARDS ────────────────────────────────────────────────────────────────
$cards = @(
    [ordered]@{ Id = (Gid 4 1); AccountId = $a1; Network = 'Visa';       Last4 = '7719'; ExpiryMonth = 12; ExpiryYear = 2028; HolderName = 'MIKA TESFAYE';  Status = 'Active';  Token = 'tok_seed_c1' }
    [ordered]@{ Id = (Gid 4 2); AccountId = $a2; Network = 'Mastercard'; Last4 = '4321'; ExpiryMonth = 6;  ExpiryYear = 2027; HolderName = 'MIKA TESFAYE';  Status = 'Active';  Token = 'tok_seed_c2' }
    [ordered]@{ Id = (Gid 4 3); AccountId = $a3; Network = 'Visa';       Last4 = '5566'; ExpiryMonth = 3;  ExpiryYear = 2029; HolderName = 'RANIA ADEL';    Status = 'Active';  Token = 'tok_seed_c3' }
    [ordered]@{ Id = (Gid 4 4); AccountId = $a4; Network = 'Meeza';      Last4 = '8812'; ExpiryMonth = 9;  ExpiryYear = 2027; HolderName = 'RANIA ADEL';    Status = 'Blocked'; Token = 'tok_seed_c4' }
    [ordered]@{ Id = (Gid 4 5); AccountId = $a5; Network = 'Visa';       Last4 = '1234'; ExpiryMonth = 8;  ExpiryYear = 2028; HolderName = 'DANIEL OKORO';  Status = 'Active';  Token = 'tok_seed_c5' }
    [ordered]@{ Id = (Gid 4 6); AccountId = $a8; Network = 'Mastercard'; Last4 = '9999'; ExpiryMonth = 1;  ExpiryYear = 2024; HolderName = 'LAYLA MANSOUR'; Status = 'Closed';  Token = 'tok_seed_c6' }
)

# ── 4. BENEFICIARIES ────────────────────────────────────────────────────────
$beneficiaries = @(
    [ordered]@{ Id = (Gid 5 1); UserId = $u1; Name = 'Omar Farouk';     TransferType = 'CardNumber'; CardNumberLast4 = '7719'; CardNumberHash = 'SHA256:4111111111117719'; AccountNumber = $null; BankId = $null;          BranchId = $null;          CreatedAt = (Fmt (At 170)) }
    [ordered]@{ Id = (Gid 5 2); UserId = $u1; Name = 'Rania Adel';      TransferType = 'SameBank';  CardNumberLast4 = $null;  CardNumberHash = $null;                      AccountNumber = '261000000003'; BankId = $BankCBE;    BranchId = $BrCBE1;     CreatedAt = (Fmt (At 160)) }
    [ordered]@{ Id = (Gid 5 3); UserId = $u2; Name = 'Daniel Okoro';    TransferType = 'OtherBank'; CardNumberLast4 = $null;  CardNumberHash = $null;                      AccountNumber = '261000000005'; BankId = $BankAwash;  BranchId = $BrAwash4;   CreatedAt = (Fmt (At 150)) }
    [ordered]@{ Id = (Gid 5 4); UserId = $u3; Name = 'Mika Tesfaye';    TransferType = 'SameBank';  CardNumberLast4 = $null;  CardNumberHash = $null;                      AccountNumber = '261000000001'; BankId = $BankCBE;    BranchId = $BrCBE1;     CreatedAt = (Fmt (At 140)) }
    [ordered]@{ Id = (Gid 5 5); UserId = $u3; Name = 'Hana Okoro';      TransferType = 'CardNumber'; CardNumberLast4 = '8812'; CardNumberHash = 'SHA256:5222222222228812'; AccountNumber = $null; BankId = $null;          BranchId = $null;          CreatedAt = (Fmt (At 130)) }
    [ordered]@{ Id = (Gid 5 6); UserId = $u6; Name = 'Hana Mansour';    TransferType = 'OtherBank'; CardNumberLast4 = $null;  CardNumberHash = $null;                      AccountNumber = '261000000002'; BankId = $BankTelebirr; BranchId = $BrTelebirr7; CreatedAt = (Fmt (At 120)) }
)

# ── 5. TRANSFERS ────────────────────────────────────────────────────────────
$transfers = @(
    [ordered]@{ Id = (Gid 6 1); FromAccountId = $a1; ToAccountId = $null; Amount = (Mo 250 'USD'); Fee = (Mo 10 'USD'); Status = 'Pending';   IdempotencyKey = 'seed-transfer-1'; Note = 'Rent share';         CreatedAt = (Fmt (At 5));  CustomerId = $u1; TransferType = 'CardNumber'; VerificationMethod = 'Otp';     BeneficiaryId = (Gid 5 1); FailureReason = $null }
    [ordered]@{ Id = (Gid 6 2); FromAccountId = $a3; ToAccountId = $null; Amount = (Mo 1500 'EGP'); Fee = (Mo 10 'EGP'); Status = 'Pending';   IdempotencyKey = 'seed-transfer-2'; Note = 'Monthly support';    CreatedAt = (Fmt (At 55)); CustomerId = $u2; TransferType = 'OtherBank'; VerificationMethod = 'TouchId'; BeneficiaryId = (Gid 5 3); FailureReason = $null }
    [ordered]@{ Id = (Gid 6 3); FromAccountId = $a5; ToAccountId = $null; Amount = (Mo 75 'USD');  Fee = (Mo 10 'USD'); Status = 'Pending';   IdempotencyKey = 'seed-transfer-3'; Note = 'Subscription';       CreatedAt = (Fmt (At 11)); CustomerId = $u3; TransferType = 'CardNumber'; VerificationMethod = 'FaceId';  BeneficiaryId = (Gid 5 5); FailureReason = $null }
    [ordered]@{ Id = (Gid 6 4); FromAccountId = $a1; ToAccountId = $null; Amount = (Mo 120 'USD'); Fee = (Mo 10 'USD'); Status = 'Failed';    IdempotencyKey = 'seed-transfer-4'; Note = 'Gadget purchase';    CreatedAt = (Fmt (At 70)); CustomerId = $u1; TransferType = 'CardNumber'; VerificationMethod = 'Otp';     BeneficiaryId = (Gid 5 1); FailureReason = 'The beneficiary card was declined by the issuing bank.' }
    [ordered]@{ Id = (Gid 6 5); FromAccountId = $a5; ToAccountId = $null; Amount = (Mo 400 'USD'); Fee = (Mo 10 'USD'); Status = 'Failed';    IdempotencyKey = 'seed-transfer-5'; Note = 'Rent to landlord';   CreatedAt = (Fmt (At 90)); CustomerId = $u3; TransferType = 'OtherBank'; VerificationMethod = 'TouchId'; BeneficiaryId = (Gid 5 4); FailureReason = 'The beneficiary account was not found at the receiving bank.' }
    [ordered]@{ Id = (Gid 6 6); FromAccountId = $a9; ToAccountId = $null; Amount = (Mo 300 'EUR'); Fee = (Mo 10 'EUR'); Status = 'Completed'; IdempotencyKey = 'seed-transfer-6'; Note = 'Family support';     CreatedAt = (Fmt (At 30)); CustomerId = $u6; TransferType = 'OtherBank'; VerificationMethod = 'Otp';     BeneficiaryId = (Gid 5 6); FailureReason = $null }
    [ordered]@{ Id = (Gid 6 7); FromAccountId = $a1; ToAccountId = $a5;   Amount = (Mo 200 'USD'); Fee = (Mo 0 'USD');  Status = 'Completed'; IdempotencyKey = 'seed-transfer-7'; Note = 'Dinner split';       CreatedAt = (Fmt (At 20)); CustomerId = $null; TransferType = 'SameBank';  VerificationMethod = 'Otp';     BeneficiaryId = $null;      FailureReason = $null }
    [ordered]@{ Id = (Gid 6 8); FromAccountId = $a5; ToAccountId = $a1;   Amount = (Mo 150 'USD'); Fee = (Mo 0 'USD');  Status = 'Completed'; IdempotencyKey = 'seed-transfer-8'; Note = 'Shared utilities';   CreatedAt = (Fmt (At 12)); CustomerId = $null; TransferType = 'SameBank';  VerificationMethod = 'Otp';     BeneficiaryId = $null;      FailureReason = $null }
)

# ── 6. BILLS ────────────────────────────────────────────────────────────────
$bills = @(
    [ordered]@{ Id = (Gid 7 1); Type = 'Electric'; Code = 'ELEC-8801'; CustomerName = 'Mika Tesfaye';   Company = 'EEUA';               PeriodFrom = (Day -36); PeriodTo = (Day -6); Fee = (Mo 450 'USD'); Tax = (Mo 30 'USD'); DueDate = (Day -6);  Status = 'Paid';   CustomerUserId = $u1 }
    [ordered]@{ Id = (Gid 7 2); Type = 'Water';    Code = 'WTR-5502';  CustomerName = 'Rania Adel';     Company = 'Cairo Water Company'; PeriodFrom = (Day -45); PeriodTo = (Day -15); Fee = (Mo 850 'EGP'); Tax = (Mo 50 'EGP'); DueDate = (Day -15); Status = 'Paid';   CustomerUserId = $u2 }
    [ordered]@{ Id = (Gid 7 3); Type = 'Internet'; Code = 'INT-7734';  CustomerName = 'Daniel Okoro';   Company = 'Ethio Telecom';       PeriodFrom = (Day -45); PeriodTo = (Day -15); Fee = (Mo 55 'USD');  Tax = (Mo 5 'USD');  DueDate = (Day -15); Status = 'Paid';   CustomerUserId = $u3 }
    [ordered]@{ Id = (Gid 7 4); Type = 'Mobile';   Code = 'MOB-8802';  CustomerName = 'Sara Haddad';    Company = 'Zain KSA';            PeriodFrom = (Day -14); PeriodTo = (Day -1);  Fee = (Mo 45 'USD');  Tax = (Mo 5 'USD');  DueDate = (Day 7);   Status = 'Unpaid'; CustomerUserId = $u4 }
    [ordered]@{ Id = (Gid 7 5); Type = 'Electric'; Code = 'ELEC-8815'; CustomerName = 'Omar Farouk';    Company = 'EEUA';                PeriodFrom = (Day -30); PeriodTo = (Day -1);  Fee = (Mo 1200 'EGP'); Tax = (Mo 120 'EGP'); DueDate = (Day 10); Status = 'Unpaid'; CustomerUserId = $u5 }
    [ordered]@{ Id = (Gid 7 6); Type = 'Water';    Code = 'WTR-5611';  CustomerName = 'Layla Mansour';  Company = 'Cairo Water Company'; PeriodFrom = (Day -30); PeriodTo = (Day -1);  Fee = (Mo 230 'EGP'); Tax = (Mo 25 'EGP'); DueDate = (Day 12); Status = 'Unpaid'; CustomerUserId = $u6 }
    [ordered]@{ Id = (Gid 7 7); Type = 'Internet'; Code = 'INT-9911';  CustomerName = 'Mika Tesfaye';   Company = 'Safaricom';           PeriodFrom = (Day -30); PeriodTo = (Day -1);  Fee = (Mo 80 'USD');  Tax = (Mo 8 'USD');  DueDate = (Day 15); Status = 'Unpaid'; CustomerUserId = $u1 }
    [ordered]@{ Id = (Gid 7 8); Type = 'Electric'; Code = 'ELEC-9922'; CustomerName = 'Rania Adel';     Company = 'EEUA';                PeriodFrom = (Day -30); PeriodTo = (Day -1);  Fee = (Mo 760 'EGP'); Tax = (Mo 70 'EGP'); DueDate = (Day 20); Status = 'Unpaid'; CustomerUserId = $u2 }
)

# ── 7. BILL PAYMENTS ────────────────────────────────────────────────────────
$billPayments = @(
    [ordered]@{ Id = (Gid 8 1); UserId = $u1; BillId = (Gid 7 1); AccountId = $a1; TransactionId = (FindTxn $a1 480 'Electric bill ELEC-8801').Id; BillType = 'Electric'; Amount = (Mo 480 'USD'); IdempotencyKey = 'seed-bp-1'; CreatedAt = (Fmt (At 6)) }
    [ordered]@{ Id = (Gid 8 2); UserId = $u2; BillId = (Gid 7 2); AccountId = $a3; TransactionId = (FindTxn $a3 900 'Water bill WTR-5502').Id;    BillType = 'Water';    Amount = (Mo 900 'EGP'); IdempotencyKey = 'seed-bp-2'; CreatedAt = (Fmt (At 45)) }
    [ordered]@{ Id = (Gid 8 3); UserId = $u3; BillId = (Gid 7 3); AccountId = $a5; TransactionId = (FindTxn $a5 60 'Internet bill INT-7734').Id;  BillType = 'Internet'; Amount = (Mo 60 'USD');  IdempotencyKey = 'seed-bp-3'; CreatedAt = (Fmt (At 45)) }
)

# ── 8. MOBILE TOP-UPS ───────────────────────────────────────────────────────
$topups = @(
    [ordered]@{ Id = (Gid 9 1); UserId = $u1; AccountId = $a1; TransactionId = (FindTxn $a1 20 'Mobile top-up').Id; PhoneNumber = '+251911000001'; Amount = (Mo 20 'USD'); IdempotencyKey = 'seed-tp-1'; CreatedAt = (Fmt (At 3)) }
    [ordered]@{ Id = (Gid 9 2); UserId = $u2; AccountId = $a3; TransactionId = (FindTxn $a3 50 'Mobile top-up').Id; PhoneNumber = '+201002200002'; Amount = (Mo 50 'EGP'); IdempotencyKey = 'seed-tp-2'; CreatedAt = (Fmt (At 8)) }
    [ordered]@{ Id = (Gid 9 3); UserId = $u3; AccountId = $a5; TransactionId = (FindTxn $a5 25 'Mobile top-up').Id; PhoneNumber = '+234803300003'; Amount = (Mo 25 'USD'); IdempotencyKey = 'seed-tp-3'; CreatedAt = (Fmt (At 4)) }
)

# ── 9. WITHDRAWALS ──────────────────────────────────────────────────────────
$wd2Created = (At 10).AddMinutes(-2)
$withdrawals = @(
    [ordered]@{ Id = (Gid 13 1); UserId = $u1; AccountId = $a1; PhoneNumber = '+251911000001'; Amount = (Mo 100 'USD'); CodeHash = 'HMAC:seed-wd-1'; Status = 'PendingVerification'; FailedVerificationAttempts = 0; CreatedAt = (Fmt (At 2)); ExpiresAt = (Fmt (At 2).AddMinutes(5)); CompletedAt = $null }
    [ordered]@{ Id = (Gid 13 2); UserId = $u1; AccountId = $a1; PhoneNumber = '+251911000001'; Amount = (Mo 60 'USD');  CodeHash = 'HMAC:seed-wd-2'; Status = 'Completed';            FailedVerificationAttempts = 0; CreatedAt = (Fmt $wd2Created); ExpiresAt = (Fmt $wd2Created.AddMinutes(5)); CompletedAt = (Fmt (At 10)) }
    [ordered]@{ Id = (Gid 13 3); UserId = $u3; AccountId = $a5; PhoneNumber = '+234803300003'; Amount = (Mo 200 'USD'); CodeHash = 'HMAC:seed-wd-3'; Status = 'Expired';              FailedVerificationAttempts = 1; CreatedAt = (Fmt (At 16)); ExpiresAt = (Fmt (At 16).AddMinutes(5)); CompletedAt = $null }
    [ordered]@{ Id = (Gid 13 4); UserId = $u3; AccountId = $a5; PhoneNumber = '+234803300003'; Amount = (Mo 150 'USD'); CodeHash = 'HMAC:seed-wd-4'; Status = 'Cancelled';            FailedVerificationAttempts = 3; CreatedAt = (Fmt (At 14)); ExpiresAt = (Fmt (At 14).AddMinutes(5)); CompletedAt = $null }
)

# ── 10. SAVINGS GOALS ───────────────────────────────────────────────────────
$goals = @(
    [ordered]@{ Id = (Gid 10 1); UserId = $u1; AccountId = $a1;  Name = 'Emergency fund'; TargetAmount = (Mo 3000 'USD');  SavedAmount = (Mo 900 'USD');   TargetDate = (Day 60); Status = 'Active' }
    [ordered]@{ Id = (Gid 10 2); UserId = $u3; AccountId = $a10; Name = 'New laptop';     TargetAmount = (Mo 1000 'USD');  SavedAmount = (Mo 400 'USD');   TargetDate = (Day 90); Status = 'Active' }
    [ordered]@{ Id = (Gid 10 3); UserId = $u2; AccountId = $a3;  Name = 'Umrah trip';     TargetAmount = (Mo 20000 'EGP'); SavedAmount = (Mo 20000 'EGP'); TargetDate = (Day 30); Status = 'Completed' }
)

# ── 11. SPENDING LIMITS ─────────────────────────────────────────────────────
$MonthStart = [DateTimeOffset]::new($GEN.Year, $GEN.Month, 1, 0, 0, 0, [TimeSpan]::Zero)
$DayStart   = [DateTimeOffset]::new($GEN.Year, $GEN.Month, $GEN.Day, 0, 0, 0, [TimeSpan]::Zero)
$limits = @(
    [ordered]@{ Id = (Gid 11 1); UserId = $u1; CardId = $null;   AccountId = $a1; Category = 'Food';          Period = 'Daily';   LimitAmount = (Mo 100 'USD'); LastHalfwayNotificationPeriodStart = $null;                     LastExceededNotificationPeriodStart = $null;   IsActive = $true }
    [ordered]@{ Id = (Gid 11 2); UserId = $u1; CardId = (Gid 4 1); AccountId = $null; Category = 'Shopping';     Period = 'Monthly'; LimitAmount = (Mo 500 'USD'); LastHalfwayNotificationPeriodStart = (Fmt $MonthStart);        LastExceededNotificationPeriodStart = $null;   IsActive = $true }
    [ordered]@{ Id = (Gid 11 3); UserId = $u2; CardId = $null;   AccountId = $a3; Category = 'Food';          Period = 'Daily';   LimitAmount = (Mo 400 'EGP'); LastHalfwayNotificationPeriodStart = $null;                     LastExceededNotificationPeriodStart = $null;   IsActive = $true }
    [ordered]@{ Id = (Gid 11 4); UserId = $u3; CardId = $null;   AccountId = $a5; Category = 'Food';          Period = 'Daily';   LimitAmount = (Mo 100 'USD'); LastHalfwayNotificationPeriodStart = $null;                     LastExceededNotificationPeriodStart = (Fmt $DayStart); IsActive = $false }
    [ordered]@{ Id = (Gid 11 5); UserId = $u3; CardId = (Gid 4 5); AccountId = $null; Category = 'Entertainment'; Period = 'Weekly';  LimitAmount = (Mo 200 'USD'); LastHalfwayNotificationPeriodStart = $null;                     LastExceededNotificationPeriodStart = $null;   IsActive = $true }
)

# ── 12. TIME DEPOSITS ───────────────────────────────────────────────────────
$deposits = @(
    [ordered]@{ Id = (Gid 12 1); UserId = $u1; FundingAccountId = $a1; Principal = (Mo 5000 'USD'); TermInMonths = 12; AnnualInterestRatePercent = 5.00; CreatedAt = (Fmt (At 120)); MaturesAt = (Fmt (At 120).AddMonths(12)); Status = 'Active' }
    [ordered]@{ Id = (Gid 12 2); UserId = $u3; FundingAccountId = $a5; Principal = (Mo 2000 'USD'); TermInMonths = 3;  AnnualInterestRatePercent = 4.00; CreatedAt = (Fmt (At 185)); MaturesAt = (Fmt (At 185).AddMonths(3));  Status = 'Redeemed' }
    [ordered]@{ Id = (Gid 12 3); UserId = $u1; FundingAccountId = $a1; Principal = (Mo 3000 'USD'); TermInMonths = 6;  AnnualInterestRatePercent = 4.50; CreatedAt = (Fmt (At 185)); MaturesAt = (Fmt (At 185).AddMonths(6));  Status = 'Matured' }
    [ordered]@{ Id = (Gid 12 4); UserId = $u2; FundingAccountId = $a3; Principal = (Mo 1500 'EGP'); TermInMonths = 3;  AnnualInterestRatePercent = 4.00; CreatedAt = (Fmt (At 3));   MaturesAt = (Fmt (At 3).AddMonths(3));    Status = 'Active' }
)

# ── 13. EXCHANGE OPERATIONS ─────────────────────────────────────────────────
$exchangeOps = @(
    [ordered]@{ Id = (Gid 14 1); UserId = $u1; SourceAccountId = $a1; TargetAccountId = $a2; SourceAmount = (Mo 150 'USD');  TargetAmount = (Mo 7500 'EGP');  ExchangeRate = 50;   CreatedAt = (Fmt (At 1)) }
    [ordered]@{ Id = (Gid 14 2); UserId = $u3; SourceAccountId = $a6; TargetAccountId = $a5; SourceAmount = (Mo 5000 'EGP'); TargetAmount = (Mo 100 'USD');   ExchangeRate = 0.02; CreatedAt = (Fmt (At 95)) }
    [ordered]@{ Id = (Gid 14 3); UserId = $u2; SourceAccountId = $a4; TargetAccountId = $a3; SourceAmount = (Mo 200 'USD');  TargetAmount = (Mo 10000 'EGP'); ExchangeRate = 50;   CreatedAt = (Fmt (At 12)) }
)

# ── 14. NOTIFICATIONS ───────────────────────────────────────────────────────
$notifications = @(
    [ordered]@{ Id = (Gid 15 1);  UserId = $u1; Type = 'TransferSent';                Title = 'Transfer sent';              Body = 'Your transfer of USD 250.00 is being processed.'; RelatedEntityId = (Gid 6 1);  IsRead = $true;  CreatedAt = (Fmt (At 5)) }
    [ordered]@{ Id = (Gid 15 2);  UserId = $u1; Type = 'PaymentSuccessful';           Title = 'Bill paid';                  Body = 'Your Electric bill ELEC-8801 was paid.';          RelatedEntityId = (Gid 8 1);  IsRead = $true;  CreatedAt = (Fmt (At 6)) }
    [ordered]@{ Id = (Gid 15 3);  UserId = $u1; Type = 'SpendingLimitHalfwayReached'; Title = 'Half of your limit used';    Body = 'You have used half of your Shopping limit.';      RelatedEntityId = (Gid 11 2); IsRead = $false; CreatedAt = (Fmt (At 1)) }
    [ordered]@{ Id = (Gid 15 4);  UserId = $u1; Type = 'CardAdded';                   Title = 'Card added';                 Body = 'A new Visa card ending 7719 was added.';          RelatedEntityId = (Gid 4 1);  IsRead = $true;  CreatedAt = (Fmt (At 170)) }
    [ordered]@{ Id = (Gid 15 5);  UserId = $u2; Type = 'PaymentSuccessful';           Title = 'Bill paid';                  Body = 'Your Water bill WTR-5502 was paid.';              RelatedEntityId = (Gid 8 2);  IsRead = $true;  CreatedAt = (Fmt (At 45)) }
    [ordered]@{ Id = (Gid 15 6);  UserId = $u2; Type = 'SavingsGoalReached';          Title = 'Savings goal reached';       Body = 'Congratulations! You reached your Umrah trip goal.'; RelatedEntityId = (Gid 10 3); IsRead = $true; CreatedAt = (Fmt (At 80)) }
    [ordered]@{ Id = (Gid 15 7);  UserId = $u3; Type = 'TransferReceived';            Title = 'Money received';             Body = 'You received USD 200.00 from Mika Tesfaye.';      RelatedEntityId = (Gid 6 7);  IsRead = $false; CreatedAt = (Fmt (At 20)) }
    [ordered]@{ Id = (Gid 15 8);  UserId = $u3; Type = 'PaymentSuccessful';           Title = 'Bill paid';                  Body = 'Your Internet bill INT-7734 was paid.';           RelatedEntityId = (Gid 8 3);  IsRead = $true;  CreatedAt = (Fmt (At 45)) }
    [ordered]@{ Id = (Gid 15 9);  UserId = $u6; Type = 'TransferSent';                Title = 'Transfer sent';              Body = 'Your transfer of EUR 300.00 has completed.';      RelatedEntityId = (Gid 6 6);  IsRead = $false; CreatedAt = (Fmt (At 30)) }
    [ordered]@{ Id = (Gid 15 10); UserId = $u2; Type = 'General';                     Title = 'Welcome to Limita';          Body = 'Thanks for joining Limita. Explore your new app.'; RelatedEntityId = $null;      IsRead = $true;  CreatedAt = (Fmt (At 180)) }
)

# ── 15. OTP CODES ───────────────────────────────────────────────────────────
$otpCodes = @(
    [ordered]@{ Id = (Gid 16 1); UserId = $u1; Purpose = 'PhoneVerification'; CodeHash = 'HMAC:seed-otp-1'; CreatedAt = (Fmt (At 1));  ExpiresAt = (Fmt (At 1).AddMinutes(5));  ConsumedAt = $null;           FailedAttempts = 0 }
    [ordered]@{ Id = (Gid 16 2); UserId = $u4; Purpose = 'PhoneVerification'; CodeHash = 'HMAC:seed-otp-2'; CreatedAt = (Fmt (At 1));  ExpiresAt = (Fmt (At 1).AddMinutes(5));  ConsumedAt = $null;           FailedAttempts = 1 }
    [ordered]@{ Id = (Gid 16 3); UserId = $u1; Purpose = 'PasswordReset';     CodeHash = 'HMAC:seed-otp-3'; CreatedAt = (Fmt (At 10)); ExpiresAt = (Fmt (At 10).AddMinutes(5)); ConsumedAt = $null;           FailedAttempts = 0 }
    [ordered]@{ Id = (Gid 16 4); UserId = $u6; Purpose = 'PasswordReset';     CodeHash = 'HMAC:seed-otp-4'; CreatedAt = (Fmt (At 8));  ExpiresAt = (Fmt (At 8).AddMinutes(5));  ConsumedAt = (Fmt (At 8));    FailedAttempts = 0 }
)

# ── 16. REFRESH TOKENS ──────────────────────────────────────────────────────
$refreshTokens = @(
    [ordered]@{ Id = (Gid 17 1); UserId = $u1; TokenHash = 'HMAC:seed-rt-1'; ExpiresAt = (Fmt (At 10).AddDays(7)); CreatedAt = (Fmt (At 10)); RevokedAt = $null }
    [ordered]@{ Id = (Gid 17 2); UserId = $u2; TokenHash = 'HMAC:seed-rt-2'; ExpiresAt = (Fmt (At 1).AddDays(7));  CreatedAt = (Fmt (At 1));  RevokedAt = $null }
    [ordered]@{ Id = (Gid 17 3); UserId = $u3; TokenHash = 'HMAC:seed-rt-3'; ExpiresAt = (Fmt (At 10).AddDays(7)); CreatedAt = (Fmt (At 10)); RevokedAt = (Fmt (At 9)) }
)

# ── 17. BRANCHES (atm map) ──────────────────────────────────────────────────
$branches = @(
    [ordered]@{ Id = (Gid 18 1); Name = 'Maadi Branch';        Address = '12 Road 9, Maadi';             City = 'Cairo';         PhoneNumber = '+20225280001'; Latitude = 29.9602; Longitude = 31.2569;  IsActive = $false }
    [ordered]@{ Id = (Gid 18 2); Name = 'Nasr City Branch';    Address = '5 Abbas El Akkad, Nasr City';  City = 'Cairo';         PhoneNumber = '+20222600002'; Latitude = 30.0596; Longitude = 31.3400;  IsActive = $true }
    [ordered]@{ Id = (Gid 18 3); Name = 'Bole Branch';         Address = 'Africa Avenue, Bole';          City = 'Addis Ababa';   PhoneNumber = '+25111660003'; Latitude = 9.0054;  Longitude = 38.7636;  IsActive = $true }
    [ordered]@{ Id = (Gid 18 4); Name = 'Ikeja Branch';        Address = '12 Allen Avenue, Ikeja';       City = 'Lagos';         PhoneNumber = '+23417700004'; Latitude = 6.6018;  Longitude = 3.3515;   IsActive = $true }
    [ordered]@{ Id = (Gid 18 5); Name = 'Deira Branch';        Address = 'Al Rigga Road, Deira';         City = 'Dubai';         PhoneNumber = '+97142200005'; Latitude = 25.2653; Longitude = 55.3230;  IsActive = $true }
    [ordered]@{ Id = (Gid 18 6); Name = 'Maadi Corniche ATM';  Address = 'Corniche El Nil, Maadi';       City = 'Cairo';         PhoneNumber = $null;          Latitude = 29.9550; Longitude = 31.2400;  IsActive = $true }
)

# ── 18. INTEREST RATES ──────────────────────────────────────────────────────
$interestRates = @(
    [ordered]@{ Id = (Gid 19 1); ProductCode = 'TD-3M';   ProductName = 'Time Deposit - 3 Months';  AnnualRatePercent = 4.00; MinimumTermMonths = 3;    MaximumTermMonths = 3;    MinimumAmount = 1000; CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
    [ordered]@{ Id = (Gid 19 2); ProductCode = 'TD-6M';   ProductName = 'Time Deposit - 6 Months';  AnnualRatePercent = 4.50; MinimumTermMonths = 6;    MaximumTermMonths = 6;    MinimumAmount = 1000; CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
    [ordered]@{ Id = (Gid 19 3); ProductCode = 'TD-12M';  ProductName = 'Time Deposit - 12 Months'; AnnualRatePercent = 5.00; MinimumTermMonths = 12;   MaximumTermMonths = 12;   MinimumAmount = 1000; CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
    [ordered]@{ Id = (Gid 19 4); ProductCode = 'TD-16M';  ProductName = 'Time Deposit - 16 Months'; AnnualRatePercent = 5.50; MinimumTermMonths = 16;   MaximumTermMonths = 16;   MinimumAmount = 1000; CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
    [ordered]@{ Id = (Gid 19 5); ProductCode = 'TD-24M';  ProductName = 'Time Deposit - 24 Months'; AnnualRatePercent = 6.00; MinimumTermMonths = 24;   MaximumTermMonths = 24;   MinimumAmount = 1000; CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $false }
    [ordered]@{ Id = (Gid 19 6); ProductCode = 'SAV-USD'; ProductName = 'USD Savings Account';      AnnualRatePercent = 2.50; MinimumTermMonths = $null; MaximumTermMonths = $null; MinimumAmount = 100;  CurrencyCode = 'USD'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
    [ordered]@{ Id = (Gid 19 7); ProductCode = 'SAV-EGP'; ProductName = 'EGP Savings Account';      AnnualRatePercent = 3.00; MinimumTermMonths = $null; MaximumTermMonths = $null; MinimumAmount = 1000; CurrencyCode = 'EGP'; EffectiveFrom = (Fmt (At 90)); IsActive = $true }
)

# ── 19. EXCHANGE RATES ──────────────────────────────────────────────────────
$exchangeRates = @(
    [ordered]@{ Id = (Gid 20 1);  BaseCurrency = 'USD'; QuoteCurrency = 'EGP'; Rate = 50;      BuyRate = 49.5;  SellRate = 50.5;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 2);  BaseCurrency = 'EUR'; QuoteCurrency = 'EGP'; Rate = 54;      BuyRate = 53.5;  SellRate = 54.5;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 3);  BaseCurrency = 'GBP'; QuoteCurrency = 'EGP'; Rate = 64;      BuyRate = 63.4;  SellRate = 64.6;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 4);  BaseCurrency = 'SAR'; QuoteCurrency = 'EGP'; Rate = 13.33;   BuyRate = 13.2;  SellRate = 13.46; UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 5);  BaseCurrency = 'AED'; QuoteCurrency = 'EGP'; Rate = 13.61;   BuyRate = 13.48; SellRate = 13.74; UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 6);  BaseCurrency = 'USD'; QuoteCurrency = 'SAR'; Rate = 3.75;    BuyRate = 3.71;  SellRate = 3.79;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 7);  BaseCurrency = 'USD'; QuoteCurrency = 'AED'; Rate = 3.67;    BuyRate = 3.63;  SellRate = 3.71;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 8);  BaseCurrency = 'EUR'; QuoteCurrency = 'USD'; Rate = 1.08;    BuyRate = 1.07;  SellRate = 1.09;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 9);  BaseCurrency = 'GBP'; QuoteCurrency = 'USD'; Rate = 1.28;    BuyRate = 1.27;  SellRate = 1.29;  UpdatedAt = (Fmt (At 1)); IsActive = $true }
    [ordered]@{ Id = (Gid 20 10); BaseCurrency = 'EGP'; QuoteCurrency = 'AED'; Rate = 0.0735;  BuyRate = $null;  SellRate = $null;  UpdatedAt = (Fmt (At 30)); IsActive = $false }
)

# ── 20. MESSAGE THREADS + MESSAGES ──────────────────────────────────────────
$threads = @(
    [ordered]@{ Id = (Gid 21 1); UserId = $u1; Subject = 'Card payment declined'; Status = 'Open';   CreatedAt = (Fmt (At 20)); UpdatedAt = (Fmt (At 18)) }
    [ordered]@{ Id = (Gid 21 2); UserId = $u2; Subject = 'Account verification';  Status = 'Closed'; CreatedAt = (Fmt (At 60)); UpdatedAt = (Fmt (At 58)) }
    [ordered]@{ Id = (Gid 21 3); UserId = $u3; Subject = 'Transfer enquiry';      Status = 'Open';   CreatedAt = (Fmt (At 10)); UpdatedAt = (Fmt (At 9)) }
)
$messages = @(
    [ordered]@{ Id = (Gid 22 1); ThreadId = (Gid 21 1); SenderUserId = $u1; Body = 'My card payment was declined yesterday.';            IsFromBank = $false; SentAt = (Fmt (At 20)) }
    [ordered]@{ Id = (Gid 22 2); ThreadId = (Gid 21 1); SenderUserId = $u1; Body = 'We are looking into it and will get back to you.';     IsFromBank = $true;  SentAt = (Fmt (At 19)) }
    [ordered]@{ Id = (Gid 22 3); ThreadId = (Gid 21 1); SenderUserId = $u1; Body = 'Thank you for the quick response.';                     IsFromBank = $false; SentAt = (Fmt (At 18)) }
    [ordered]@{ Id = (Gid 22 4); ThreadId = (Gid 21 2); SenderUserId = $u2; Body = 'How do I verify my account?';                          IsFromBank = $false; SentAt = (Fmt (At 60)) }
    [ordered]@{ Id = (Gid 22 5); ThreadId = (Gid 21 2); SenderUserId = $u2; Body = 'Please upload your ID through the app.';                 IsFromBank = $true;  SentAt = (Fmt (At 59)) }
    [ordered]@{ Id = (Gid 22 6); ThreadId = (Gid 21 2); SenderUserId = $u2; Body = 'Done, thanks.';                                      IsFromBank = $false; SentAt = (Fmt (At 58)) }
    [ordered]@{ Id = (Gid 22 7); ThreadId = (Gid 21 3); SenderUserId = $u3; Body = 'Why is my transfer still pending?';                   IsFromBank = $false; SentAt = (Fmt (At 10)) }
    [ordered]@{ Id = (Gid 22 8); ThreadId = (Gid 21 3); SenderUserId = $u3; Body = 'External transfers can take up to one business day.';  IsFromBank = $true;  SentAt = (Fmt (At 9)) }
    [ordered]@{ Id = (Gid 22 9); ThreadId = (Gid 21 3); SenderUserId = $u3; Body = 'I understand now.';                                  IsFromBank = $false; SentAt = (Fmt (At 9)) }
)

# ── EXPORT ──────────────────────────────────────────────────────────────────
$seed = [ordered]@{
    Users              = $users
    Accounts           = $accounts
    Transactions       = $transactions
    Cards              = $cards
    Beneficiaries      = $beneficiaries
    Transfers          = $transfers
    Bills              = $bills
    BillPayments       = $billPayments
    MobileTopUps       = $topups
    SavingsGoals       = $goals
    SpendingLimits     = $limits
    TimeDeposits       = $deposits
    Withdrawals        = $withdrawals
    ExchangeOperations = $exchangeOps
    Notifications      = $notifications
    OtpCodes           = $otpCodes
    RefreshTokens      = $refreshTokens
    Branches           = $branches
    InterestRates      = $interestRates
    ExchangeRates      = $exchangeRates
    MessageThreads     = $threads
    Messages           = $messages
}

$json = $seed | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($OutFile, $json, [System.Text.UTF8Encoding]::new($false))
Write-Host ("Wrote {0} ({1} bytes)" -f $OutFile, (Get-Item -LiteralPath $OutFile).Length)


# Development mock seed data

This folder holds the hand-crafted, fictional dataset that the API loads into an empty database when it
runs in the **Development** environment, plus the tools used to produce and check it.

## Files

| File | Purpose |
| --- | --- |
| `seed-data.json` | The dataset itself. Keys match the `ApplicationDbContext` DbSet names; every object matches the corresponding domain entity. |
| `New-SeedData.ps1` | Generates `seed-data.json` from scratch. Run this to change the data. |
| `Test-SeedData.ps1` | Validates `seed-data.json` against the schema rules (shapes, enums, foreign keys, uniqueness, money and date rules, ledger balance consistency). |
| `SeedDataFileSeeder.cs` | Loads the JSON at startup (see below). |

## How it is loaded

`DevelopmentDataSeeder.EnsureAsync` (called from `Program.cs` in Development only) migrates the database,
ensures the small developer account, and then calls `SeedDataFileSeeder.EnsureAsync`.

The seeder:

- does nothing when the marker user `mika.tesfaye@limita.test` already exists (**idempotent**);
- materialises each row through the entity's private constructor and private setters, so the file stays a
  plain description of the resulting rows;
- inserts everything in a single `SaveChanges`;
- is skipped silently when `seed-data.json` is not found.

To re-seed from scratch, delete the seeded users (or drop the database) and run the API in Development again.

## The login credentials

Every seeded user signs in with the password `DevSeed#2026`. Emails use the `@limita.test` domain:

| User | Email | Notes |
| --- | --- | --- |
| Mika Tesfaye | `mika.tesfaye@limita.test` | marker user; accounts, cards and savings goals |
| Rania Adel | `rania.adel@limita.test` | |
| Daniel Okoro | `daniel.okoro@limita.test` | |
| Sara Haddad | `sara.haddad@limita.test` | phone not verified |
| Omar Farouk | `omar.farouk@limita.test` | currently locked out |
| Layla Mansour | `layla.mansour@limita.test` | Arabic UI (`ar`), a few failed logins |

## Secrets are sentinels, not hashes

The JSON never stores a real hash. Values that must be hashed carry a prefix that the seeder resolves at
load time:

| Prefix | Meaning | Resolved with |
| --- | --- | --- |
| `HASH:<password>` | A user password | `IPasswordHasher` (ASP.NET Core Identity) |
| `HMAC:<raw>` | An OTP code, withdrawal code or refresh token | `ISecretHasher` (HMAC-SHA256) |
| `SHA256:<raw>` | A beneficiary full card number | plain SHA-256, uppercase hex |

So `"PasswordHash": "HASH:DevSeed#2026"` becomes a real PBKDF2 hash when the data is loaded.

## Regenerating and validating

```powershell
# regenerate seed-data.json (no database needed; dates are relative to today)
powershell -NoProfile -ExecutionPolicy Bypass -File .\Development\SeedData\New-SeedData.ps1

# check the result
powershell -NoProfile -ExecutionPolicy Bypass -File .\Development\SeedData\Test-SeedData.ps1
```

`Test-SeedData.ps1` prints `PASS - seed-data.json is valid.` when everything is consistent.

## Assumptions worth knowing

- The dataset contains **one completed external transfer**. The application code only ever creates external
  transfers in the `Pending` or `Failed` states; the completed one exists purely so the transfer history has a
  settled example. It is data-only and is not produced by any command.
- IDs follow a readable pattern: `{namespace:d8}-0000-4000-8000-{counter:d12}` (users `00000001…`,
  accounts `00000002…`, and so on).
- Bank and branch foreign keys reuse the real seeded `BankSeed` identifiers; the extra branches here are
  mock rows for the branch list.

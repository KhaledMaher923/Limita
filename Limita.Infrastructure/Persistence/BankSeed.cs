using Limita.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Limita.Infrastructure.Persistence;

/// <summary>Seeds banks and branches so the transfer form has data to work with.</summary>
public static class BankSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // ── Banks ────────────────────────────────────────────────
        var bankCbe = new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "Commercial Bank of Ethiopia", SwiftCode = "CBETETAA", CreatedAt = now };
        var bankAwash = new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "Awash Bank", SwiftCode = "AABORETT", CreatedAt = now };
        var bankAbyssinia = new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "Bank of Abyssinia", SwiftCode = "AABORETY", CreatedAt = now };
        var bankDashen = new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "Dashen Bank", SwiftCode = "DASHETAA", CreatedAt = now };
        var bankTelebirr = new { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), Name = "Telebirr", SwiftCode = "TELEBIAA", CreatedAt = now };

        modelBuilder.Entity<Bank>().HasData(bankCbe, bankAwash, bankAbyssinia, bankDashen, bankTelebirr);

        // ── Branches ──────────────────────────────────────────────
        modelBuilder.Entity<BankBranch>().HasData(
            // CBE
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"), BankId = bankCbe.Id, Name = "CBE Main Branch", Address = "Addis Ababa, Churchill Ave", CreatedAt = now },
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"), BankId = bankCbe.Id, Name = "CBE Bole Branch", Address = "Addis Ababa, Bole Road", CreatedAt = now },
            // Awash
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"), BankId = bankAwash.Id, Name = "Awash HQ Branch", Address = "Addis Ababa, Ras Desta St", CreatedAt = now },
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"), BankId = bankAwash.Id, Name = "Awash Megenagna Branch", Address = "Addis Ababa, Megenagna", CreatedAt = now },
            // Abyssinia
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000005"), BankId = bankAbyssinia.Id, Name = "Abyssinia Main Branch", Address = "Addis Ababa, Meskel Square", CreatedAt = now },
            // Dashen
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000006"), BankId = bankDashen.Id, Name = "Dashen Piassa Branch", Address = "Addis Ababa, Piassa", CreatedAt = now },
            // Telebirr
            new { Id = Guid.Parse("b1000000-0000-0000-0000-000000000007"), BankId = bankTelebirr.Id, Name = "Telebirr Digital Branch", Address = "Online", CreatedAt = now }
        );
    }
}

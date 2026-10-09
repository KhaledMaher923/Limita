using Limita.Domain.Entities;
using Limita.Domain.Enums;
using Limita.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Limita.Infrastructure.Persistence
{
    public static class BillSeed
    {
        public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            if (await db.Bills.AnyAsync(ct))
                return;

            // بنربط الفواتير بأول مستخدم موجود عشان Smart Pay يلاقي فواتير
            var customerId = await db.Users
                .OrderBy(u => u.CreatedAt)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var periodFrom = today.AddMonths(-1);

            Money Usd(decimal amount) => Money.Of(amount, "USD");

            db.Bills.AddRange(
                // Figma: fee 470 + tax 10 = 480
                Bill.Create(BillType.Electric, "2468", "Jackson Maine", "City Power",
                    periodFrom, today, Usd(470), Usd(10), today.AddDays(5), customerId),

                // Figma (history): 280
                Bill.Create(BillType.Water, "WTR-1001", "Jackson Maine", "City Water",
                    periodFrom, today, Usd(270), Usd(10), today.AddDays(7), customerId),

                // Figma: fee 50 + tax 10 = 60, Capi Telecom
                Bill.Create(BillType.Internet, "2343543", "Jackson Maine", "Capi Telecom",
                    periodFrom, today, Usd(50), Usd(10), today.AddDays(3), customerId),

                Bill.Create(BillType.Mobile, "MOB-1001", "Jackson Maine", "Capi Telecom",
                    periodFrom, today, Usd(45), Usd(5), today.AddDays(10), customerId));

            await db.SaveChangesAsync(ct);
        }
    }
}
using Limita.Domain.Entities;

namespace Limita.Application.Features.Bills;

public sealed record BillDto(
    Guid Id,
    string Type,
    string Code,
    string CustomerName,
    string? Company,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal Fee,
    decimal Tax,
    decimal Total,
    string Currency,
    DateOnly DueDate,
    string Status);

internal static class BillMappings
{
    // Bill.Total is calculated in code, so the mapping runs in memory after the entity is loaded.
    public static BillDto ToDto(this Bill bill) => new(
        bill.Id,
        bill.Type.ToString(),
        bill.Code,
        bill.CustomerName,
        bill.Company,
        bill.PeriodFrom,
        bill.PeriodTo,
        bill.Fee.Amount,
        bill.Tax.Amount,
        bill.Total.Amount,
        bill.Fee.Currency,
        bill.DueDate,
        bill.Status.ToString());
}

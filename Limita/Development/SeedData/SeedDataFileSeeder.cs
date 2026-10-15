using Limita.Application.Common.Abstractions;
using Limita.Domain.ValueObjects;
using Limita.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Limita.Development;

/// <summary>
/// Development-only seeder that loads the hand-crafted mock dataset in
/// "Development/SeedData/seed-data.json" into an empty database.
///
/// The JSON keys match the <see cref="ApplicationDbContext"/> DbSet names and the properties match the
/// domain entities. Values that are secrets are stored as sentinels so the file never holds a real
/// hash: "HASH:&lt;password&gt;" (password hasher), "HMAC:&lt;raw&gt;" (HMAC secret hasher) and
/// "SHA256:&lt;raw&gt;" (plain SHA-256, used for the beneficiary card-number hash).
///
/// The seeder is idempotent: it does nothing when the marker user already exists. Entities are created
/// through their (private) parameterless constructor and their private setters are filled in by
/// reflection, so the file stays a plain description of the resulting rows.
/// </summary>
public static class SeedDataFileSeeder
{
    public const string MarkerEmail = "mika.tesfaye@limita.test";

    private const string RelativePath = "Development/SeedData/seed-data.json";

    public static async Task EnsureAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Limita.Development.SeedDataFileSeeder");

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        if (await dbContext.Users.AnyAsync(item => item.Email == MarkerEmail))
            return;

        var path = ResolveFile(scope.ServiceProvider);

        if (path is null)
        {
            logger.LogWarning("Seed data file '{Path}' was not found; skipping JSON seed.", RelativePath);
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var secretHasher = scope.ServiceProvider.GetRequiredService<ISecretHasher>();

        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream);

        var sets = GetDbSetTypes(dbContext);
        var count = 0;

        // The JSON document is written in dependency-safe order (parents before children), and EF Core
        // still topologically sorts the inserts, so a single SaveChanges is enough.
        foreach (var collection in document.RootElement.EnumerateObject())
        {
            if (!sets.TryGetValue(collection.Name, out var entityType))
            {
                logger.LogWarning("Seed data collection '{Collection}' does not map to a DbSet; skipping.", collection.Name);
                continue;
            }

            foreach (var element in collection.Value.EnumerateArray())
            {
                var entity = Materialize(entityType, element, passwordHasher, secretHasher);
                dbContext.Add(entity);
                count++;
            }
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} rows from '{Path}'.", count, path);
    }

    private static string? ResolveFile(IServiceProvider provider)
    {
        var environment = provider.GetRequiredService<IHostEnvironment>();

        var candidates = new[]
        {
            Path.Combine(environment.ContentRootPath, "Development", "SeedData", "seed-data.json"),
            Path.Combine(AppContext.BaseDirectory, "Development", "SeedData", "seed-data.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Dictionary<string, Type> GetDbSetTypes(ApplicationDbContext dbContext)
        => typeof(ApplicationDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToDictionary(
                property => property.Name,
                property => property.PropertyType.GetGenericArguments()[0],
                StringComparer.Ordinal);

    private static object Materialize(
        Type entityType,
        JsonElement json,
        IPasswordHasher passwordHasher,
        ISecretHasher secretHasher)
    {
        var instance = Activator.CreateInstance(entityType, nonPublic: true)
            ?? throw new InvalidOperationException($"Cannot create {entityType.Name}.");

        foreach (var property in json.EnumerateObject())
        {
            var info = entityType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);

            if (info is null || info.SetMethod is null)
                continue;

            var value = ConvertValue(info.PropertyType, property.Value, passwordHasher, secretHasher);
            info.SetValue(instance, value);
        }

        return instance;
    }

    private static object? ConvertValue(
        Type target,
        JsonElement value,
        IPasswordHasher passwordHasher,
        ISecretHasher secretHasher)
    {
        var underlying = Nullable.GetUnderlyingType(target);

        if (value.ValueKind == JsonValueKind.Null)
            return null;

        if (underlying is not null)
            return ConvertValue(underlying, value, passwordHasher, secretHasher);

        if (target == typeof(string))
            return ResolveSentinel(value.GetString(), passwordHasher, secretHasher);

        if (target == typeof(Guid))
            return Guid.Parse(value.GetString()!);

        if (target == typeof(bool))
            return value.GetBoolean();

        if (target == typeof(int))
            return value.GetInt32();

        if (target == typeof(long))
            return value.GetInt64();

        if (target == typeof(decimal))
            return value.GetDecimal();

        if (target == typeof(DateTimeOffset))
            return value.GetDateTimeOffset();

        if (target == typeof(DateOnly))
            return DateOnly.ParseExact(value.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (target == typeof(Money))
            return Money.Of(
                value.GetProperty("Amount").GetDecimal(),
                value.GetProperty("Currency").GetString()!);

        if (target.IsEnum)
            return Enum.Parse(target, value.GetString()!, ignoreCase: false);

        throw new InvalidOperationException($"Seed data cannot write a value into '{target.Name}'.");
    }

    private static string? ResolveSentinel(string? raw, IPasswordHasher passwordHasher, ISecretHasher secretHasher)
    {
        if (raw is null)
            return null;

        if (raw.StartsWith("HASH:", StringComparison.Ordinal))
            return passwordHasher.Hash(raw["HASH:".Length..]);

        if (raw.StartsWith("HMAC:", StringComparison.Ordinal))
            return secretHasher.Hash(raw["HMAC:".Length..]);

        if (raw.StartsWith("SHA256:", StringComparison.Ordinal))
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw["SHA256:".Length..])));

        return raw;
    }
}

using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class Language : Entity
{
    private Language() { }

    public string Code { get; private set; } = string.Empty;
    public string NativeName { get; private set; } = string.Empty;
    public string EnglishName { get; private set; } = string.Empty;
    public bool IsRightToLeft { get; private set; }
    public bool IsEnabled { get; private set; }

    public static Language Create(string code, string nativeName, string englishName, bool isRightToLeft) => new()
    {
        Code = Guard.NotBlank(code, "Code", 10).ToLowerInvariant(),
        NativeName = Guard.NotBlank(nativeName, "Native name", 60),
        EnglishName = Guard.NotBlank(englishName, "English name", 60),
        IsRightToLeft = isRightToLeft,
        IsEnabled = true
    };
}

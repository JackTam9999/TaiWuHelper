using TaiWu.Application.Localization;

namespace TaiWuAPI.Localization;

public sealed class TaiwuLanguageState
{
    public TaiwuLanguage Current { get; private set; } =
        TaiwuLanguage.Chinese;

    public void Set(TaiwuLanguage language)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.");
        }

        Current = language;
    }
}

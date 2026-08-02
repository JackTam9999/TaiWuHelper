namespace TaiWu.Infrastructure.SaveGames;

internal static class StoryReportSection
{
    public static void Write(TaiwuReportContext context)
    {
        context.Writer.Write("RANSHAN|unavailable=standalone-runtime");
        context.Writer.Write("SECTSTORY|unavailable=standalone-runtime");
    }
}

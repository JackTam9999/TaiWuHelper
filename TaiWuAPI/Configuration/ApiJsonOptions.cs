using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace TaiWuAPI.Configuration;

internal static class ApiJsonOptions
{
    public static void Configure(JsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false));
    }
}

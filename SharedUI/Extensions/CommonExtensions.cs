namespace Moba.SharedUI.Extensions;

/// <summary>
/// Extension methods for common string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null, empty, or whitespace.
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Truncates a string to a maximum length and adds ellipsis if needed.
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length before truncation</param>
    /// <returns>Truncated string with ellipsis if needed</returns>
    public static string Truncate(this string? value, int maxLength)
    {
        if (value == null) return string.Empty;
        if (maxLength < 0) throw new ArgumentException("Max length must be positive", nameof(maxLength));
        
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    /// <summary>
    /// Converts a string to Title Case (first letter of each word capitalized).
    /// </summary>
    public static string ToTitleCase(this string? value)
    {
        if (value.IsNullOrEmpty()) return string.Empty;
        
        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value!.ToLower());
    }
}

/// <summary>
/// Extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Performs an action on each element in a collection.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (var item in collection)
        {
            action(item);
        }
    }
}

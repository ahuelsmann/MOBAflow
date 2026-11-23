// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Build;

using System.Text.RegularExpressions;

/// <summary>Patterns that find the local user or machine name of the build machine in delivered files.</summary>
internal static class HostDetail
{
    /// <summary>Returns the patterns for a user or machine name; names shorter than three characters yield none.</summary>
    internal static IEnumerable<Regex> NamePatterns(string name)
    {
        if (name.Length < 3)
        {
            yield break;
        }

        // A short or generic name (root, dev, user, node) is also an ordinary word in a front-end bundle, so it only
        // counts in a path or host context: a home folder, a UNC share, a URL host or a local host name.
        var escaped = Regex.Escape(name);
        foreach (var pattern in new[]
                 {
                     @"(?:Users|home)[\\/]" + escaped + @"(?![\w.-])",
                     @"\\\\" + escaped + @"[\\/]",
                     @"://" + escaped + @"(?![\w.-])",
                     @"(?<![\w.-])" + escaped + @"\.(?:local|lan|internal)\b",
                 })
        {
            yield return new Regex(
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(10));
        }
    }
}

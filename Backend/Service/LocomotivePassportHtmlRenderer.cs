// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using System.Net;
using System.Text;

public interface ILocomotivePassportHtmlRenderer
{
    string Render(LocomotivePassport passport);
}

public sealed class LocomotivePassportHtmlRenderer : ILocomotivePassportHtmlRenderer
{
    public string Render(LocomotivePassport passport)
    {
        ArgumentNullException.ThrowIfNull(passport);
        static string Encode(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "—");

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        builder.AppendLine("<title>Locomotive passport</title>");
        builder.AppendLine("<style>body{font-family:system-ui,sans-serif;max-width:760px;margin:3rem auto;color:#172033}h1{border-bottom:3px solid #d6502f;padding-bottom:.4rem}dl{display:grid;grid-template-columns:12rem 1fr;gap:.65rem}dt{font-weight:700}@media print{body{margin:1cm}}</style></head><body>");
        builder.Append("<h1>").Append(Encode(passport.Name)).AppendLine("</h1><dl>");
        AppendRow(builder, "Locomotive ID", passport.LocomotiveId);
        AppendRow(builder, "Digital address", passport.DigitalAddress);
        AppendRow(builder, "Manufacturer", passport.Manufacturer);
        AppendRow(builder, "Article number", passport.ArticleNumber);
        AppendRow(builder, "Decoder", passport.Decoder is null ? null : $"{passport.Decoder.Manufacturer} {passport.Decoder.Model}".Trim());
        AppendRow(builder, "Decoder protocol", passport.Decoder?.Protocol);
        AppendRow(builder, "CV snapshots", passport.DecoderSnapshotCount);
        AppendRow(builder, "Maintenance state", passport.MaintenanceState);
        AppendRow(builder, "Latest maintenance", passport.LatestMaintenance?.Description);
        builder.AppendLine("</dl></body></html>");
        return builder.ToString();

        static void AppendRow(StringBuilder output, string label, object? value)
        {
            output.Append("<dt>").Append(Encode(label)).Append("</dt><dd>").Append(Encode(value)).AppendLine("</dd>");
        }
    }
}

// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Enum;

using System.Text.Json;

/// <summary>
/// Migrates legacy workflow action <c>parameters</c> objects into typed payload properties.
/// </summary>
public static class WorkflowActionLegacyParameterMigrator
{
    public static void Merge(WorkflowAction action, JsonElement legacyParams)
    {
        if (action.Type == ActionType.Command &&
            (TryGetInsensitive(legacyParams, "FilePath", out _) || TryGetInsensitive(legacyParams, "AudioFile", out _)))
        {
            action.Type = ActionType.Audio;
        }

        WorkflowActionPayloadDescriptors.Find(action.Type)?.MergeLegacy(action, legacyParams);
    }

    private static bool TryGetInsensitive(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
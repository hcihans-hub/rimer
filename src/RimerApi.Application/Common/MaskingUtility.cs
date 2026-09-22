using System;
using System.Collections.Generic;
using System.Linq;

namespace RimerApi.Application.Common;

/// <summary>
/// Centralized Masking Strategy for Audit Logging and Observability.
/// Runs entirely in memory before serialization to ensure no secrets ever leak.
/// </summary>
public static class MaskingUtility
{
    private static readonly string[] FullMaskFields = { "password", "token", "secret", "key", "authorization", "hash", "salt" };
    private static readonly string[] PartialMaskFields = { "email", "phone" };

    /// <summary>
    /// Evaluates key-value pairs and returns a new dictionary with sensitive values masked.
    /// </summary>
    public static Dictionary<string, object?> MaskDictionary(IReadOnlyDictionary<string, object?> payload, int maxFieldLength)
    {
        var maskedDict = new Dictionary<string, object?>();

        foreach (var kvp in payload)
        {
            maskedDict[kvp.Key] = MaskValue(kvp.Key, kvp.Value, maxFieldLength);
        }

        return maskedDict;
    }

    private static object? MaskValue(string key, object? value, int maxFieldLength)
    {
        if (value == null) return null;

        // Recursive Dictionary traversal
        if (value is IReadOnlyDictionary<string, object?> nestedDict)
        {
            return MaskDictionary(nestedDict, maxFieldLength);
        }

        var strValue = value.ToString();
        if (string.IsNullOrEmpty(strValue)) return strValue;

        // Recursive JSON object evaluation
        if (strValue.TrimStart().StartsWith("{") && strValue.TrimEnd().EndsWith("}"))
        {
            try 
            {
                var parsedDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(strValue);
                if (parsedDict != null) {
                    var maskedSub = MaskDictionary(parsedDict, maxFieldLength);
                    return System.Text.Json.JsonSerializer.Serialize(maskedSub);
                }
            } catch { /* Ignored if malformed JSON */ }
        }

        // Value-based Masking (Intercepting Bearer tokens or secrets by value characteristics)
        if (strValue.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) || 
            strValue.Contains("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return "***REDACTED_AUTHORIZATION***";
        }

        // 1. Full Masking Check (بالكامل)
        if (FullMaskFields.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            return "***REDACTED***";
        }

        // 2. Partial Masking Check
        if (PartialMaskFields.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            if (key.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                var atIndex = strValue.IndexOf('@');
                if (atIndex > 1) return strValue.Substring(0, 1) + "***" + strValue.Substring(atIndex);
                return "***REDACTED***";
            }
            if (key.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                return strValue.Length >= 3 ? strValue.Substring(0, 3) + "***" : "***REDACTED***";
            }
        }

        // 3. Truncation for Safety (MaxPropertyLength safeguard)
        if (strValue.Length > maxFieldLength)
        {
            return strValue.Substring(0, maxFieldLength) + "...";
        }

        return value;
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace QRTracker.UITests;

internal static class TestCredentials
{
    private const string FileName = "credentials.txt";
    private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyCredentials = new(LoadCredentials);
    private static string? _cachedPath;

    internal static string Get(string key)
    {
        if (LazyCredentials.Value.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Credential '{key}' is missing or empty in '{GetCredentialsPath()}'.");
    }

    private static IReadOnlyDictionary<string, string> LoadCredentials()
    {
        var path = GetCredentialsPath();
        var lines = File.ReadAllLines(path);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid line in credentials file '{path}': '{rawLine}'. Expected format 'Key = Value'.");
            }

            var key = parts[0];
            var value = parts[1];

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException($"Credential key is missing in line '{rawLine}' of '{path}'.");
            }

            if (dict.ContainsKey(key))
            {
                throw new InvalidOperationException($"Duplicate credential key '{key}' detected in '{path}'.");
            }

            dict[key] = value;
        }

        return dict;
    }

    private static string GetCredentialsPath()
    {
        if (!string.IsNullOrWhiteSpace(_cachedPath))
        {
            return _cachedPath!;
        }

        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            var candidate = Path.Combine(directory, FileName);
            if (File.Exists(candidate))
            {
                _cachedPath = candidate;
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException($"Could not locate '{FileName}' starting from '{AppContext.BaseDirectory}'. Ensure it is placed in the solution root.");
    }
}

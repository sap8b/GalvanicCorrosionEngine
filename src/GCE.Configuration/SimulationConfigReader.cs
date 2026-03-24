using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GCE.Configuration;

/// <summary>
/// Reads <see cref="SimulationConfig"/> objects from JSON or YAML text / files.
/// </summary>
public sealed class SimulationConfigReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly IDeserializer YamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .IgnoreUnmatchedProperties()
            .Build();

    // ── JSON ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a <see cref="SimulationConfig"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>The deserialised <see cref="SimulationConfig"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
    public SimulationConfig ReadJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<SimulationConfig>(json, JsonOptions)
               ?? throw new JsonException("JSON deserialization returned null; ensure the input is a valid JSON object.");
    }

    /// <summary>
    /// Reads and parses a <see cref="SimulationConfig"/> from a JSON file.
    /// </summary>
    /// <param name="path">Path to the JSON configuration file.</param>
    /// <returns>The deserialised <see cref="SimulationConfig"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public SimulationConfig ReadJsonFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: '{path}'.", path);

        return ReadJson(File.ReadAllText(path));
    }

    // ── YAML ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a <see cref="SimulationConfig"/> from a YAML string.
    /// </summary>
    /// <param name="yaml">The YAML text to parse.</param>
    /// <returns>The deserialised <see cref="SimulationConfig"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yaml"/> is null.</exception>
    public SimulationConfig ReadYaml(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        return YamlDeserializer.Deserialize<SimulationConfig>(yaml)
               ?? new SimulationConfig();
    }

    /// <summary>
    /// Reads and parses a <see cref="SimulationConfig"/> from a YAML file.
    /// </summary>
    /// <param name="path">Path to the YAML configuration file.</param>
    /// <returns>The deserialised <see cref="SimulationConfig"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public SimulationConfig ReadYamlFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: '{path}'.", path);

        return ReadYaml(File.ReadAllText(path));
    }

    // ── Auto-detect ───────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a <see cref="SimulationConfig"/> from a file, detecting the format
    /// from the file extension (<c>.json</c> → JSON; <c>.yaml</c> / <c>.yml</c> → YAML).
    /// </summary>
    /// <param name="path">Path to the configuration file.</param>
    /// <returns>The deserialised <see cref="SimulationConfig"/>.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the file extension is not recognised.
    /// </exception>
    public SimulationConfig ReadFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".json" => ReadJsonFile(path),
            ".yaml" or ".yml" => ReadYamlFile(path),
            _ => throw new NotSupportedException(
                $"Unsupported configuration file extension '{ext}'. " +
                "Supported extensions are: .json, .yaml, .yml"),
        };
    }
}

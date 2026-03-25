namespace GCE.Configuration;

/// <summary>
/// Exception thrown when a <see cref="SimulationConfig"/> fails validation.
/// </summary>
public sealed class ConfigurationValidationException : Exception
{
    /// <summary>Gets the list of validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initialises a new <see cref="ConfigurationValidationException"/> with the given errors.
    /// </summary>
    /// <param name="errors">One or more human-readable validation error messages.</param>
    public ConfigurationValidationException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"Configuration validation failed with {errors.Count} error(s):");
        for (int i = 0; i < errors.Count; i++)
            lines.AppendLine($"  {i + 1}. {errors[i]}");

        return lines.ToString().TrimEnd();
    }
}

using GCE.Simulation;

namespace GCE.IO;

/// <summary>
/// Abstraction over the various result-export formats supported by <c>GCE.IO</c>.
/// </summary>
/// <remarks>
/// All implementations write a <see cref="SimulationResult"/> to either a
/// <see cref="TextWriter"/> or directly to a file path.  Implementations may
/// optionally accept additional context (e.g. a <see cref="GCE.Core.GeometryMesh"/>)
/// through their constructors.
/// </remarks>
public interface IResultWriter
{
    /// <summary>
    /// Writes the simulation result to <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">The simulation result to write.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> or <paramref name="writer"/> is
    /// <see langword="null"/>.
    /// </exception>
    void Write(SimulationResult result, TextWriter writer);

    /// <summary>
    /// Writes the simulation result to the file at <paramref name="filePath"/>,
    /// creating or overwriting the file.
    /// </summary>
    /// <param name="result">The simulation result to write.</param>
    /// <param name="filePath">Destination file path.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> or <paramref name="filePath"/> is
    /// <see langword="null"/>.
    /// </exception>
    void WriteToFile(SimulationResult result, string filePath);
}

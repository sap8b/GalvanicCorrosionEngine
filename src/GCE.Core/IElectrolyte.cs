namespace GCE.Core;

/// <summary>
/// Represents an electrolyte solution that mediates ion transport between electrodes.
/// </summary>
/// <remarks>
/// Extends <see cref="IEnvironment"/> with the bulk ionic concentration of the solution.
/// </remarks>
public interface IElectrolyte : IEnvironment
{
    /// <summary>Gets the total ionic concentration of the electrolyte (mol/L).</summary>
    double Concentration { get; }
}

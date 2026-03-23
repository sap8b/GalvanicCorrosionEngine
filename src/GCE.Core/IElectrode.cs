namespace GCE.Core;

/// <summary>
/// Represents an electrode — a conducting material in electrochemical contact with an electrolyte.
/// </summary>
public interface IElectrode
{
    /// <summary>Gets the material this electrode is made from.</summary>
    IMaterial Material { get; }

    /// <summary>Gets the electrochemically active surface area of the electrode (m²).</summary>
    double Area { get; }
}

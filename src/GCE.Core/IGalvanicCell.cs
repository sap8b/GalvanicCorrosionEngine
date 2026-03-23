namespace GCE.Core;

/// <summary>
/// Represents a complete galvanic cell consisting of two dissimilar electrodes
/// and a shared electrolyte.
/// </summary>
public interface IGalvanicCell
{
    /// <summary>Gets the anodic electrode (more active / more negative standard potential).</summary>
    IElectrode Anode { get; }

    /// <summary>Gets the cathodic electrode (more noble / more positive standard potential).</summary>
    IElectrode Cathode { get; }

    /// <summary>Gets the electrolyte medium connecting the two electrodes.</summary>
    IElectrolyte Electrolyte { get; }

    /// <summary>
    /// Gets the open-circuit galvanic voltage (V): E_cathode − E_anode.
    /// </summary>
    double GalvanicVoltage { get; }
}

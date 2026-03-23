using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Concrete implementation of <see cref="IGalvanicCell"/> that couples two electrodes
/// through a shared electrolyte.
/// </summary>
public sealed class GalvanicCell : IGalvanicCell
{
    /// <inheritdoc/>
    public IElectrode Anode { get; }

    /// <inheritdoc/>
    public IElectrode Cathode { get; }

    /// <inheritdoc/>
    public IElectrolyte Electrolyte { get; }

    /// <inheritdoc/>
    public double GalvanicVoltage =>
        Cathode.Material.StandardPotential - Anode.Material.StandardPotential;

    /// <param name="anode">The anodic electrode.</param>
    /// <param name="cathode">The cathodic electrode.</param>
    /// <param name="electrolyte">The shared electrolyte medium.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the cathode's standard potential is not higher than the anode's.
    /// </exception>
    public GalvanicCell(IElectrode anode, IElectrode cathode, IElectrolyte electrolyte)
    {
        if (cathode.Material.StandardPotential <= anode.Material.StandardPotential)
            throw new ArgumentException(
                "Cathode standard potential must be strictly higher than the anode standard potential.");

        Anode = anode;
        Cathode = cathode;
        Electrolyte = electrolyte;
    }
}

using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Represents a galvanic pair of two dissimilar metals in electrical contact
/// within a common electrolyte.
/// </summary>
public sealed class GalvanicPair
{
    /// <summary>Gets the anodic (more active / negative potential) material.</summary>
    public IMaterial Anode { get; }

    /// <summary>Gets the cathodic (more noble / positive potential) material.</summary>
    public IMaterial Cathode { get; }

    /// <summary>
    /// Gets the open-circuit galvanic voltage (V): E_cathode − E_anode.
    /// </summary>
    public double GalvanicVoltage => Cathode.StandardPotential - Anode.StandardPotential;

    /// <param name="anode">The anodic material.</param>
    /// <param name="cathode">The cathodic material.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the cathode potential is not higher than the anode potential.
    /// </exception>
    public GalvanicPair(IMaterial anode, IMaterial cathode)
    {
        if (cathode.StandardPotential <= anode.StandardPotential)
            throw new ArgumentException(
                "Cathode must have a higher standard potential than the anode.");

        Anode = anode;
        Cathode = cathode;
    }
}

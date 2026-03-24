using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Represents a galvanic couple — two dissimilar electrodes (<see cref="Anode"/> and
/// <see cref="Cathode"/>) connected through a common electrolyte.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MixedPotential"/> (corrosion potential) is determined numerically by
/// finding the potential at which the total current from all coupled electrodes sums to
/// zero, accounting for each electrode's surface area:
/// </para>
/// <code>A_anode · i_anode(E_m) + A_cathode · i_cathode(E_m) = 0</code>
/// <para>
/// Bisection is used over the interval [<see cref="Anode.OpenCircuitPotential"/>,
/// <see cref="Cathode.OpenCircuitPotential"/>].
/// </para>
/// </remarks>
public sealed class GalvanicCouple
{
    /// <summary>Gets the anodic electrode (metal dissolution).</summary>
    public Anode Anode { get; }

    /// <summary>Gets the cathodic electrode (reduction reactions).</summary>
    public Cathode Cathode { get; }

    /// <summary>Gets the shared electrolyte medium.</summary>
    public IElectrolyte Electrolyte { get; }

    /// <summary>
    /// Gets the open-circuit galvanic voltage (V): cathode OCP − anode OCP.
    /// </summary>
    public double GalvanicVoltage =>
        Cathode.OpenCircuitPotential - Anode.OpenCircuitPotential;

    /// <param name="anode">The anodic electrode.</param>
    /// <param name="cathode">The cathodic electrode.</param>
    /// <param name="electrolyte">The shared electrolyte medium.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the cathode open-circuit potential is not strictly higher than the anode's.
    /// </exception>
    public GalvanicCouple(Anode anode, Cathode cathode, IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(anode);
        ArgumentNullException.ThrowIfNull(cathode);
        ArgumentNullException.ThrowIfNull(electrolyte);

        if (cathode.OpenCircuitPotential <= anode.OpenCircuitPotential)
            throw new ArgumentException(
                "Cathode open-circuit potential must be strictly higher than the anode open-circuit potential.");

        Anode = anode;
        Cathode = cathode;
        Electrolyte = electrolyte;
    }

    /// <summary>
    /// Gets the mixed (corrosion) potential (V vs. SHE) where the total electrode current
    /// is zero.  Computed via bisection over [anode OCP, cathode OCP].
    /// </summary>
    public double MixedPotential => ComputeMixedPotential();

    /// <summary>
    /// Gets the corrosion current density at the anode surface (A/m²) evaluated at the
    /// <see cref="MixedPotential"/>.
    /// </summary>
    public double CorrosionCurrentDensity => Anode.ComputeCurrentDensity(MixedPotential);

    /// <summary>
    /// Generates a polarization curve for the coupled system over the specified potential range.
    /// Each point reports the area-averaged net current density of the system.
    /// </summary>
    /// <param name="startPotential">Start potential in V vs. SHE.</param>
    /// <param name="endPotential">End potential in V vs. SHE.</param>
    /// <param name="points">Number of evenly spaced sample points (minimum 2).</param>
    /// <returns>
    /// A list of <c>(Potential, TotalCurrentDensity)</c> tuples.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="points"/> is less than 2.
    /// </exception>
    public IReadOnlyList<(double Potential, double TotalCurrentDensity)> PolarizationCurve(
        double startPotential, double endPotential, int points = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points, 2, nameof(points));

        double totalArea = Anode.Area + Cathode.Area;
        var curve = new List<(double, double)>(points);
        double step = (endPotential - startPotential) / (points - 1);

        for (int i = 0; i < points; i++)
        {
            double e = startPotential + i * step;
            double totalCurrent =
                Anode.Area * Anode.ComputeCurrentDensity(e) +
                Cathode.Area * Cathode.ComputeCurrentDensity(e);
            curve.Add((e, totalCurrent / totalArea));
        }

        return curve;
    }

    // ── Mixed-potential solver ─────────────────────────────────────────────────

    private double ComputeMixedPotential(int maxIterations = 1000, double tolerance = 1e-9)
    {
        double lo = Anode.OpenCircuitPotential;
        double hi = Cathode.OpenCircuitPotential;

        // Net current: positive contribution from anode + negative from cathode.
        double f(double e) =>
            Anode.Area * Anode.ComputeCurrentDensity(e) +
            Cathode.Area * Cathode.ComputeCurrentDensity(e);

        double fLo = f(lo);
        double fHi = f(hi);

        // f(lo) should be negative (anode near OCP, cathode strongly cathodic).
        // f(hi) should be positive (anode strongly anodic, cathode near OCP).
        // If signs are the same the interval doesn't bracket a root — return midpoint.
        if (fLo * fHi > 0)
            return (lo + hi) / 2.0;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            double mid = (lo + hi) / 2.0;
            double fMid = f(mid);

            if (Math.Abs(fMid) < tolerance || (hi - lo) < 2.0 * tolerance)
                return mid;

            if (fLo * fMid < 0)
            {
                hi = mid;
                fHi = fMid;
            }
            else
            {
                lo = mid;
                fLo = fMid;
            }
        }

        return (lo + hi) / 2.0;
    }
}

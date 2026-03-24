using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Represents an anodic electrode where metal dissolution (oxidation) is the primary
/// electrochemical process.
/// </summary>
/// <remarks>
/// The primary kinetics are modelled by a <see cref="ButlerVolmerModel"/>.
/// Additional reactions (e.g. a secondary anodic process) can be registered via
/// <see cref="AddReaction"/>.  The total current density is the sum of all reactions.
/// </remarks>
public sealed class Anode : IElectrode
{
    private readonly ButlerVolmerModel _kinetics;
    private readonly List<IElectrochemicalReaction> _additionalReactions = new();

    /// <inheritdoc/>
    public IMaterial Material { get; }

    /// <inheritdoc/>
    public double Area { get; }

    /// <summary>
    /// Gets the open-circuit (equilibrium) potential of this anode (V vs. SHE).
    /// Equal to the material's standard potential.
    /// </summary>
    public double OpenCircuitPotential => Material.StandardPotential;

    /// <summary>Gets the additional reactions registered on this anode.</summary>
    public IReadOnlyList<IElectrochemicalReaction> AdditionalReactions => _additionalReactions;

    /// <param name="material">The material this anode is made from.</param>
    /// <param name="area">Electrochemically active surface area in m² (must be positive).</param>
    /// <param name="environment">The electrochemical environment for kinetics calculations.</param>
    /// <param name="alpha">Anodic charge-transfer coefficient (default 0.5).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="material"/> or <paramref name="environment"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="area"/> is not positive.
    /// </exception>
    public Anode(IMaterial material, double area, IEnvironment environment, double alpha = 0.5)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(area, 0.0, nameof(area));

        Material = material;
        Area = area;
        _kinetics = new ButlerVolmerModel(material, environment, alpha);
    }

    /// <summary>
    /// Registers an additional electrochemical reaction on this anode.
    /// </summary>
    /// <param name="reaction">The reaction to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reaction"/> is null.</exception>
    public void AddReaction(IElectrochemicalReaction reaction)
    {
        ArgumentNullException.ThrowIfNull(reaction);
        _additionalReactions.Add(reaction);
    }

    /// <summary>
    /// Computes the total current density (A/m²) at the given electrode potential.
    /// Sums the primary Butler–Volmer dissolution current and all additional reaction currents.
    /// </summary>
    /// <param name="potential">Electrode potential in V vs. SHE.</param>
    /// <returns>Net current density in A/m².</returns>
    public double ComputeCurrentDensity(double potential)
    {
        double total = _kinetics.ComputeCurrentDensity(potential);
        foreach (var rxn in _additionalReactions)
            total += rxn.CurrentDensity(potential);
        return total;
    }

    /// <summary>
    /// Generates a polarization curve (potential vs. current density) over the specified range.
    /// </summary>
    /// <param name="startPotential">Start potential in V vs. SHE.</param>
    /// <param name="endPotential">End potential in V vs. SHE.</param>
    /// <param name="points">Number of evenly spaced sample points (minimum 2).</param>
    /// <returns>
    /// A list of <c>(Potential, CurrentDensity)</c> tuples in ascending potential order.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="points"/> is less than 2.
    /// </exception>
    public IReadOnlyList<(double Potential, double CurrentDensity)> PolarizationCurve(
        double startPotential, double endPotential, int points = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points, 2, nameof(points));

        var curve = new List<(double, double)>(points);
        double step = (endPotential - startPotential) / (points - 1);
        for (int i = 0; i < points; i++)
        {
            double e = startPotential + i * step;
            curve.Add((e, ComputeCurrentDensity(e)));
        }
        return curve;
    }
}

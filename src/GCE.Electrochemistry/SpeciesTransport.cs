using GCE.Numerics.Solvers;

namespace GCE.Electrochemistry;

/// <summary>
/// Models 1-D diffusive transport of a single <see cref="Species"/> through
/// a spatial domain (e.g. across a thin electrolyte film or a diffusion layer).
/// </summary>
/// <remarks>
/// <para>
/// The governing equation is the 1-D diffusion equation:
/// <code>∂c/∂t = D · ∂²c/∂x²</code>
/// solved using the Crank–Nicolson scheme in <see cref="DiffusionSolver1D"/>,
/// which is unconditionally stable and second-order accurate.
/// </para>
/// <para>
/// The domain is discretised with <see cref="GridPoints"/> uniform nodes
/// over [0, <see cref="DomainLength"/>].  Boundary conditions are set at
/// construction via <see cref="IBoundaryCondition"/> instances; a typical
/// choice is a fixed concentration (Dirichlet) at both ends.
/// </para>
/// <para>
/// After each call to <see cref="Advance"/>, the <see cref="Species"/>
/// concentration is updated to the spatially-averaged value of the
/// concentration profile.  Access the full spatial profile via
/// <see cref="ConcentrationProfile"/>.
/// </para>
/// </remarks>
public sealed class SpeciesTransport
{
    private readonly DiffusionSolver1D _solver;
    private double[] _profile;

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <param name="species">
    /// The species whose transport is modelled.  Its
    /// <see cref="Species.DiffusionCoefficient"/> is used as D.
    /// </param>
    /// <param name="domainLength">Physical length of the 1-D domain in metres (must be &gt; 0).</param>
    /// <param name="gridPoints">Number of uniform grid nodes (must be ≥ 2).</param>
    /// <param name="initialProfile">
    /// Nodal concentration values at t = 0 (mol/m³).  Must have length
    /// <paramref name="gridPoints"/>.
    /// </param>
    /// <param name="leftBC">Boundary condition at x = 0.</param>
    /// <param name="rightBC">Boundary condition at x = <paramref name="domainLength"/>.</param>
    /// <param name="timeStep">Initial time-step size in seconds (must be &gt; 0).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any reference argument is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="domainLength"/>, <paramref name="gridPoints"/>,
    /// or <paramref name="timeStep"/> is invalid.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialProfile"/> length does not match
    /// <paramref name="gridPoints"/>.
    /// </exception>
    public SpeciesTransport(
        Species            species,
        double             domainLength,
        int                gridPoints,
        double[]           initialProfile,
        IBoundaryCondition leftBC,
        IBoundaryCondition rightBC,
        double             timeStep)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(initialProfile);
        ArgumentNullException.ThrowIfNull(leftBC);
        ArgumentNullException.ThrowIfNull(rightBC);

        if (initialProfile.Length != gridPoints)
            throw new ArgumentException(
                $"initialProfile must have length {gridPoints}.", nameof(initialProfile));

        Species     = species;
        DomainLength = domainLength;
        GridPoints  = gridPoints;

        _profile = (double[])initialProfile.Clone();

        _solver = new DiffusionSolver1D(
            nx:               gridPoints,
            domainLength:     domainLength,
            diffusivity:      species.DiffusionCoefficient,
            initialCondition: initialProfile,
            leftBC:           leftBC,
            rightBC:          rightBC,
            dt:               timeStep);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the species being transported.</summary>
    public Species Species { get; }

    /// <summary>Gets the physical length of the spatial domain (m).</summary>
    public double DomainLength { get; }

    /// <summary>Gets the number of uniform grid nodes.</summary>
    public int GridPoints { get; }

    /// <summary>
    /// Gets the current simulation time (s) — the total time elapsed since
    /// construction.
    /// </summary>
    public double CurrentTime => _solver.CurrentTime;

    /// <summary>
    /// Gets the current nodal concentration profile (mol/m³) as an array in
    /// increasing-x order.
    /// </summary>
    public IReadOnlyList<double> ConcentrationProfile => _profile;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the species concentration field by the specified number of time steps
    /// and updates <see cref="Species.Concentration"/> to the spatially-averaged value.
    /// </summary>
    /// <param name="steps">Number of time steps to advance (default 1).</param>
    /// <returns>
    /// <see langword="true"/> when the solver reports convergence (steady state);
    /// <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="steps"/> is less than 1.
    /// </exception>
    public bool Advance(int steps = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1, nameof(steps));

        var options = new PdeSolverOptions { MaxTimeSteps = steps };
        var result  = _solver.Solve(options);

        _profile = result.Solution ?? _profile;

        // Update the species' bulk concentration to the spatial average
        Species.Concentration = ComputeAverage(_profile);

        return result.Converged;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static double ComputeAverage(double[] profile)
    {
        if (profile.Length == 0)
            return 0.0;

        double sum = 0.0;
        foreach (double v in profile)
            sum += v;
        return sum / profile.Length;
    }
}

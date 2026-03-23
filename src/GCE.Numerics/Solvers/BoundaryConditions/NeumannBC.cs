namespace GCE.Numerics.Solvers;

/// <summary>
/// A Neumann boundary condition that prescribes the outward normal derivative
/// (flux) on the boundary: ∂u/∂n = q(t).
/// </summary>
/// <remarks>
/// Neumann (natural) conditions specify the rate of change of the dependent
/// variable in the direction normal to the boundary.  In electrochemical
/// simulations this corresponds to a prescribed current density or mass-flux at
/// the electrode surface.  A zero-flux Neumann condition models an insulating or
/// symmetry boundary.
/// </remarks>
public sealed class NeumannBC : IBoundaryCondition
{
    private readonly Func<double, double> _fluxFunc;

    /// <inheritdoc/>
    public BoundaryConditionType Type => BoundaryConditionType.Neumann;

    /// <summary>
    /// Initialises a constant Neumann boundary condition with the given flux value.
    /// </summary>
    /// <param name="flux">The constant prescribed outward normal flux.</param>
    public NeumannBC(double flux) : this(_ => flux) { }

    /// <summary>
    /// Initialises a time-varying Neumann boundary condition.
    /// </summary>
    /// <param name="fluxFunc">
    /// A function that returns the prescribed outward normal flux at any given
    /// simulation time (seconds).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fluxFunc"/> is <see langword="null"/>.
    /// </exception>
    public NeumannBC(Func<double, double> fluxFunc)
    {
        ArgumentNullException.ThrowIfNull(fluxFunc);
        _fluxFunc = fluxFunc;
    }

    /// <summary>
    /// Returns the prescribed outward normal flux q(t) at the given time.
    /// </summary>
    /// <param name="time">Simulation time in seconds.</param>
    /// <returns>The Neumann flux at <paramref name="time"/>.</returns>
    public double Evaluate(double time) => _fluxFunc(time);
}

namespace GCE.Numerics.Solvers;

/// <summary>
/// A Dirichlet boundary condition that prescribes the solution value directly on
/// the boundary: u = g(t).
/// </summary>
/// <remarks>
/// Dirichlet (essential) conditions fix the dependent variable itself.  In
/// electrochemical simulations this corresponds to a prescribed potential or
/// concentration at the electrode surface.
/// </remarks>
public sealed class DirichletBC : IBoundaryCondition
{
    private readonly Func<double, double> _valueFunc;

    /// <inheritdoc/>
    public BoundaryConditionType Type => BoundaryConditionType.Dirichlet;

    /// <summary>
    /// Initialises a constant Dirichlet boundary condition with the given value.
    /// </summary>
    /// <param name="value">The constant prescribed solution value.</param>
    public DirichletBC(double value) : this(_ => value) { }

    /// <summary>
    /// Initialises a time-varying Dirichlet boundary condition.
    /// </summary>
    /// <param name="valueFunc">
    /// A function that returns the prescribed solution value at any given
    /// simulation time (seconds).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="valueFunc"/> is <see langword="null"/>.
    /// </exception>
    public DirichletBC(Func<double, double> valueFunc)
    {
        ArgumentNullException.ThrowIfNull(valueFunc);
        _valueFunc = valueFunc;
    }

    /// <summary>
    /// Returns the prescribed solution value g(t) at the given time.
    /// </summary>
    /// <param name="time">Simulation time in seconds.</param>
    /// <returns>The Dirichlet value at <paramref name="time"/>.</returns>
    public double Evaluate(double time) => _valueFunc(time);
}

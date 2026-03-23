namespace GCE.Numerics.Solvers;

/// <summary>
/// A Robin boundary condition that imposes a linear combination of the solution
/// value and its outward normal derivative on the boundary:
/// α·u + β·(∂u/∂n) = γ(t).
/// </summary>
/// <remarks>
/// <para>
/// Robin (mixed or convective) conditions generalise both Dirichlet and Neumann
/// conditions:
/// <list type="bullet">
///   <item><description>
///     Setting β = 0 reduces to a Dirichlet condition with prescribed value γ/α.
///   </description></item>
///   <item><description>
///     Setting α = 0 reduces to a Neumann condition with prescribed flux γ/β.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// In electrochemical simulations, Robin conditions model convective mass transfer
/// or linearised Butler–Volmer kinetics at an electrode surface where both the
/// concentration and the flux contribute to the boundary constraint.
/// </para>
/// </remarks>
public sealed class RobinBC : IBoundaryCondition
{
    private readonly Func<double, double> _gammaFunc;

    /// <inheritdoc/>
    public BoundaryConditionType Type => BoundaryConditionType.Robin;

    /// <summary>
    /// Gets the coefficient α of the solution value u in the Robin equation
    /// α·u + β·(∂u/∂n) = γ(t).
    /// </summary>
    public double Alpha { get; }

    /// <summary>
    /// Gets the coefficient β of the outward normal derivative ∂u/∂n in the Robin
    /// equation α·u + β·(∂u/∂n) = γ(t).
    /// </summary>
    public double Beta { get; }

    /// <summary>
    /// Initialises a constant Robin boundary condition.
    /// </summary>
    /// <param name="alpha">Coefficient of the solution value u.</param>
    /// <param name="beta">Coefficient of the outward normal derivative ∂u/∂n.</param>
    /// <param name="gamma">The constant right-hand-side value γ.</param>
    public RobinBC(double alpha, double beta, double gamma)
        : this(alpha, beta, _ => gamma) { }

    /// <summary>
    /// Initialises a time-varying Robin boundary condition.
    /// </summary>
    /// <param name="alpha">Coefficient of the solution value u.</param>
    /// <param name="beta">Coefficient of the outward normal derivative ∂u/∂n.</param>
    /// <param name="gammaFunc">
    /// A function that returns the right-hand-side value γ at any given simulation
    /// time (seconds).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="gammaFunc"/> is <see langword="null"/>.
    /// </exception>
    public RobinBC(double alpha, double beta, Func<double, double> gammaFunc)
    {
        ArgumentNullException.ThrowIfNull(gammaFunc);
        Alpha = alpha;
        Beta = beta;
        _gammaFunc = gammaFunc;
    }

    /// <summary>
    /// Returns the right-hand-side value γ(t) of the Robin equation at the given
    /// time.
    /// </summary>
    /// <param name="time">Simulation time in seconds.</param>
    /// <returns>The Robin right-hand-side value γ at <paramref name="time"/>.</returns>
    public double Evaluate(double time) => _gammaFunc(time);
}

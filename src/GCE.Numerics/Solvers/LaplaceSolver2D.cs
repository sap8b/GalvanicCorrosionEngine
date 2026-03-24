namespace GCE.Numerics.Solvers;

/// <summary>
/// Solves the 2-D Laplace equation
///   ∇²u = 0
/// on a uniform rectangular grid [0, Lx] × [0, Ly] using a
/// Gauss–Seidel successive over-relaxation (SOR) iterative method.
/// </summary>
/// <remarks>
/// <para>
/// The Laplace equation is the homogeneous special case of the Poisson equation
/// (source term f = 0).  This class delegates to <see cref="PoissonSolver2D"/>
/// with a zero source function, providing a focused API for potential-field
/// problems such as steady-state electric potential in galvanic corrosion
/// simulations.
/// </para>
/// <para>
/// Grid layout: <c>nx × ny</c> nodes at positions (x_i, y_j) = (i·Δx, j·Δy),
/// i = 0 … nx − 1, j = 0 … ny − 1.
/// The flat solution array in <see cref="PdeSolverResult.Solution"/> uses
/// <em>row-major</em> order: index = i·ny + j.
/// </para>
/// <para>
/// All three boundary condition types — Dirichlet, Neumann, and Robin — are
/// supported on each of the four sides.  See <see cref="PoissonSolver2D"/> for
/// a full description of the SOR algorithm and boundary-condition handling.
/// </para>
/// </remarks>
public sealed class LaplaceSolver2D : IPDESolver
{
    private readonly PoissonSolver2D _inner;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the Laplace solver.
    /// </summary>
    /// <param name="nx">Number of grid nodes in the x-direction (must be ≥ 2).</param>
    /// <param name="ny">Number of grid nodes in the y-direction (must be ≥ 2).</param>
    /// <param name="domainLengthX">Physical width Lx (must be &gt; 0).</param>
    /// <param name="domainLengthY">Physical height Ly (must be &gt; 0).</param>
    /// <param name="leftBC">Boundary condition on the x = 0 face.</param>
    /// <param name="rightBC">Boundary condition on the x = Lx face.</param>
    /// <param name="bottomBC">Boundary condition on the y = 0 face.</param>
    /// <param name="topBC">Boundary condition on the y = Ly face.</param>
    /// <param name="initialGuess">
    /// Optional flat array (row-major, length nx·ny) used as the starting
    /// guess for the iterative solver.  When <see langword="null"/> the
    /// solution is initialised to zero (Dirichlet nodes are overwritten).
    /// </param>
    /// <param name="omega">
    /// SOR relaxation parameter ω ∈ [1, 2].  ω = 1 gives standard
    /// Gauss–Seidel; values up to 2 provide successive over-relaxation.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any dimension, domain length, or ω is out of range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any boundary condition is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialGuess"/> has the wrong length.
    /// </exception>
    public LaplaceSolver2D(
        int                  nx,
        int                  ny,
        double               domainLengthX,
        double               domainLengthY,
        IBoundaryCondition   leftBC,
        IBoundaryCondition   rightBC,
        IBoundaryCondition   bottomBC,
        IBoundaryCondition   topBC,
        double[]?            initialGuess = null,
        double               omega        = 1.0)
    {
        _inner = new PoissonSolver2D(
            nx, ny, domainLengthX, domainLengthY,
            leftBC, rightBC, bottomBC, topBC,
            source:       (_, _) => 0.0,
            initialGuess: initialGuess,
            omega:        omega);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current nodal solution as a read-only span in row-major order
    /// (index = i·ny + j).
    /// </summary>
    public ReadOnlySpan<double> CurrentSolution => _inner.CurrentSolution;

    // ── IPDESolver ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public PdeSolverResult Solve(PdeSolverOptions? options = null) =>
        _inner.Solve(options);
}

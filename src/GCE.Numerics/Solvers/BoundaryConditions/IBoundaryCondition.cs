namespace GCE.Numerics.Solvers;

/// <summary>
/// Represents a boundary condition that can be applied to a PDE solver.
/// </summary>
/// <remarks>
/// <para>
/// A boundary condition constrains the solution on the boundary of the
/// computational domain.  The three canonical types — Dirichlet, Neumann, and
/// Robin — are identified by the <see cref="Type"/> property, allowing solvers to
/// dispatch appropriately without requiring down-casting.
/// </para>
/// <para>
/// All boundary conditions support time-varying values through
/// <see cref="Evaluate"/>.  For steady-state problems, pass
/// <c>time = 0.0</c> (or any constant) to retrieve the static value.
/// </para>
/// </remarks>
public interface IBoundaryCondition
{
    /// <summary>
    /// Gets the mathematical type of this boundary condition.
    /// </summary>
    BoundaryConditionType Type { get; }

    /// <summary>
    /// Evaluates the primary scalar value of this boundary condition at the given
    /// time.
    /// </summary>
    /// <param name="time">
    /// The simulation time (seconds).  For steady-state problems pass
    /// <c>0.0</c>.
    /// </param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="BoundaryConditionType.Dirichlet"/>: the prescribed solution
    ///     value g(t).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="BoundaryConditionType.Neumann"/>: the prescribed outward
    ///     normal flux q(t).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="BoundaryConditionType.Robin"/>: the right-hand-side value
    ///     γ(t) of the Robin equation α·u + β·(∂u/∂n) = γ(t).
    ///   </description></item>
    /// </list>
    /// </returns>
    double Evaluate(double time);
}

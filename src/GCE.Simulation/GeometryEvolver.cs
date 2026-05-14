using GCE.Core;

namespace GCE.Simulation;

/// <summary>
/// Manages the slow (geometric) timescale in operator-split galvanic simulations.
/// </summary>
/// <remarks>
/// <para>
/// Galvanic corrosion operates across two vastly different timescales: the
/// electrochemical response (potential equilibration) is quasi-instantaneous (~ms),
/// while the corrosion geometry evolves over hours or days.  Advancing both on the
/// same small time step wastes computation and can obscure the physics.
/// </para>
/// <para>
/// <see cref="GeometryEvolver"/> provides the outer (slow) loop of the operator-split
/// scheme.  At each outer step it:
/// <list type="number">
///   <item><description>
///     Computes an adaptive macro-timestep Δt_geo via
///     <see cref="ComputeGeometricTimestep"/> so that at most
///     <see cref="MaxNodesPerStep"/> <see cref="NodePhase.Anode"/> nodes dissolve.
///     This is the moving-boundary stability criterion.
///   </description></item>
///   <item><description>
///     Accumulates the corresponding dissolved mass and transitions saturated nodes
///     to <see cref="NodePhase.Electrolyte"/> via <see cref="Advance"/>.
///   </description></item>
/// </list>
/// The inner (fast) loop — solving the Laplace/Poisson field to convergence at
/// fixed geometry — is performed by <see cref="SpatialSolver"/> and is unchanged.
/// </para>
/// </remarks>
public sealed class GeometryEvolver
{
    private const double SecondsPerYear = 3.156e7;

    /// <summary>
    /// Gets the maximum number of <see cref="NodePhase.Anode"/> nodes allowed to
    /// transition to <see cref="NodePhase.Electrolyte"/> within a single geometric
    /// step.  This is the moving-boundary stability criterion.  Must be at least 1.
    /// </summary>
    public int MaxNodesPerStep { get; }

    /// <summary>Gets the total number of geometric steps taken since construction.</summary>
    public int TotalGeoSteps { get; private set; }

    /// <summary>
    /// Initialises a new <see cref="GeometryEvolver"/>.
    /// </summary>
    /// <param name="maxNodesPerStep">
    /// Maximum number of anode nodes allowed to dissolve per geometric step.
    /// Must be ≥ 1. Default is 1 (most conservative stability criterion).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxNodesPerStep"/> is less than 1.
    /// </exception>
    public GeometryEvolver(int maxNodesPerStep = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxNodesPerStep, 1, nameof(maxNodesPerStep));
        MaxNodesPerStep = maxNodesPerStep;
    }

    /// <summary>
    /// Computes the adaptive geometric timestep Δt_geo such that at most
    /// <see cref="MaxNodesPerStep"/> anode nodes will dissolve in the next step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each active <see cref="NodePhase.Anode"/> node the remaining dissolution
    /// time is estimated from the current corrosion rate and accumulated mass loss:
    /// </para>
    /// <code>
    /// t_remaining = (ρ · A_node − m_accumulated) / (r_mm/yr · ρ · A_node / (1000 · s/yr))
    /// </code>
    /// <para>
    /// The remaining times are sorted ascending.  The
    /// <see cref="MaxNodesPerStep"/>-th smallest value is returned, clamped to
    /// [<paramref name="minDt"/>, <paramref name="maxDt"/>].  When fewer than
    /// <see cref="MaxNodesPerStep"/> active anode nodes remain,
    /// <paramref name="maxDt"/> is returned.
    /// </para>
    /// </remarks>
    /// <param name="mesh">The spatial mesh.</param>
    /// <param name="nodeMassLoss">
    /// Per-node cumulative dissolved mass (kg/m), flattened row-major
    /// (index = i·ny + j).
    /// </param>
    /// <param name="nodalCorrosionRates">
    /// Per-node corrosion rate (mm/year), same layout as <paramref name="nodeMassLoss"/>.
    /// </param>
    /// <param name="anodeMaterial">Anode material, providing density for the threshold.</param>
    /// <param name="minDt">
    /// Minimum returned timestep (s).  Must be positive.  Default 1 s.
    /// </param>
    /// <param name="maxDt">
    /// Maximum returned timestep (s).  Must be positive.  Default 86 400 s (1 day).
    /// </param>
    /// <returns>
    /// The adaptive geometric timestep (s), clamped to
    /// [<paramref name="minDt"/>, <paramref name="maxDt"/>].
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any reference argument is <see langword="null"/>.
    /// </exception>
    public double ComputeGeometricTimestep(
        GeometryMesh mesh,
        double[]     nodeMassLoss,
        double[]     nodalCorrosionRates,
        IMaterial    anodeMaterial,
        double       minDt = 1.0,
        double       maxDt = 86_400.0)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(nodeMassLoss);
        ArgumentNullException.ThrowIfNull(nodalCorrosionRates);
        ArgumentNullException.ThrowIfNull(anodeMaterial);

        // Normalize bounds: callers may pass maxDt < minDt when very little
        // simulation time remains (e.g. remainingTime < minDt on the final step).
        // Reducing minDt to match maxDt prevents Math.Clamp from throwing and
        // returns the largest step still within the remaining time.
        if (maxDt < minDt)
            minDt = maxDt;

        int    nx        = mesh.NodesX;
        int    ny        = mesh.NodesY;
        double dx        = nx > 1 ? (mesh.XCoordinates[nx - 1] - mesh.XCoordinates[0]) / (nx - 1) : 1.0;
        double dy        = ny > 1 ? (mesh.YCoordinates[ny - 1] - mesh.YCoordinates[0]) / (ny - 1) : 1.0;
        double aNode     = dx * dy;
        double threshold = anodeMaterial.Density * aNode;

        var remainingTimes = new List<double>();

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (mesh.Regions[i, j] != NodePhase.Anode)
                    continue;

                int    idx  = i * ny + j;
                double rate = nodalCorrosionRates[idx]; // mm/year

                if (rate <= 0.0)
                    continue;

                double remaining = threshold - nodeMassLoss[idx];
                if (remaining <= 0.0)
                    continue; // node is already at or beyond dissolution threshold

                // mass loss rate (kg/m·s) = rate_mm/yr × ρ × A_node / (1000 mm/m × s/yr)
                double massRatePerSec = rate * anodeMaterial.Density * aNode
                                        / (SecondsPerYear * 1000.0);

                remainingTimes.Add(remaining / massRatePerSec);
            }
        }

        if (remainingTimes.Count == 0)
            return maxDt;

        remainingTimes.Sort();

        // The MaxNodesPerStep-th smallest remaining time (0-indexed: index = MaxNodesPerStep-1).
        int    targetIndex = Math.Min(MaxNodesPerStep - 1, remainingTimes.Count - 1);
        double dtGeo       = remainingTimes[targetIndex];

        return Math.Clamp(dtGeo, minDt, maxDt);
    }

    /// <summary>
    /// Advances the geometry by <paramref name="dt"/> seconds: accumulates dissolved
    /// mass at each active anode node and transitions saturated nodes to
    /// <see cref="NodePhase.Electrolyte"/>.
    /// </summary>
    /// <remarks>
    /// The mass increment for node (i, j) is:
    /// <code>
    /// Δm = rate_mm/yr / (1000 mm/m × s/yr) × ρ × A_node × Δt
    /// </code>
    /// A node dissolves when its cumulative mass loss reaches the nodal threshold
    /// ρ × A_node (mass per unit depth).
    /// </remarks>
    /// <param name="mesh">The spatial mesh, mutated in place when nodes dissolve.</param>
    /// <param name="nodeMassLoss">
    /// Per-node cumulative dissolved mass (kg/m); mutated in place.
    /// </param>
    /// <param name="nodalCorrosionRates">Per-node corrosion rate (mm/year).</param>
    /// <param name="dt">Geometric timestep (s). Must be positive.</param>
    /// <param name="anodeMaterial">Anode material.</param>
    /// <returns>
    /// The number of nodes that transitioned to <see cref="NodePhase.Electrolyte"/>
    /// during this step.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any reference argument is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dt"/> is not positive.
    /// </exception>
    public int Advance(
        GeometryMesh mesh,
        double[]     nodeMassLoss,
        double[]     nodalCorrosionRates,
        double       dt,
        IMaterial    anodeMaterial)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(nodeMassLoss);
        ArgumentNullException.ThrowIfNull(nodalCorrosionRates);
        ArgumentNullException.ThrowIfNull(anodeMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dt, 0.0, nameof(dt));

        int    nx        = mesh.NodesX;
        int    ny        = mesh.NodesY;
        double dx        = nx > 1 ? (mesh.XCoordinates[nx - 1] - mesh.XCoordinates[0]) / (nx - 1) : 1.0;
        double dy        = ny > 1 ? (mesh.YCoordinates[ny - 1] - mesh.YCoordinates[0]) / (ny - 1) : 1.0;
        double aNode     = dx * dy;
        double threshold = anodeMaterial.Density * aNode;

        int transitioned = 0;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (mesh.Regions[i, j] != NodePhase.Anode)
                    continue;

                int idx = i * ny + j;

                // Δm = rate_mm/yr / (1000 × s/yr) × ρ × A_node × Δt
                double deltaMass = nodalCorrosionRates[idx] / (SecondsPerYear * 1000.0)
                                   * anodeMaterial.Density * aNode * dt;
                nodeMassLoss[idx] += deltaMass;

                if (nodeMassLoss[idx] >= threshold)
                {
                    mesh.Regions[i, j] = NodePhase.Electrolyte;
                    transitioned++;
                }
            }
        }

        TotalGeoSteps++;
        return transitioned;
    }
}

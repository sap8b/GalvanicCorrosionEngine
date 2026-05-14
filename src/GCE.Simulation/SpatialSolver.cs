using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics.Solvers;

namespace GCE.Simulation;

/// <summary>
/// Computes the steady-state spatial distribution of electrolyte potential and
/// corrosion rate on a <see cref="GeometryMesh"/> by solving the variable-
/// conductivity 2-D Laplace equation ∇·(κ∇φ) = 0.
/// </summary>
/// <remarks>
/// <para>
/// Boundary conditions are derived from the galvanic couple's anode and cathode
/// standard potentials (Dirichlet on each face; the anode face is set to the
/// anode's standard potential and the cathode face to the cathode's standard
/// potential).  All other faces carry zero-flux (Neumann) conditions.
/// </para>
/// <para>
/// The local current density is computed from the potential gradient:
///   j = −κ · ∂φ/∂x (for a side-by-side geometry)
/// and the corrosion rate is derived from Faraday's law using the anode material
/// properties.
/// </para>
/// </remarks>
internal static class SpatialSolver
{
    private const double SecondsPerYear = 3.156e7;
    private const double MetalConductivity = 1.0e6;
    private const double DefaultElectrolyteConductivity = 1.0e-3;
    private const double CorrosionProductConductivityRatio = 1.0e-2;
    private const double MinimumCorrosionProductConductivity = 1.0e-6;

    /// <summary>
    /// Solves the variable-conductivity Laplace equation on <paramref name="mesh"/>
    /// and returns the nodal potential field and nodal corrosion rates.
    /// </summary>
    /// <param name="mesh">The spatial mesh to solve on.</param>
    /// <param name="anodePotential">
    /// Dirichlet value (V vs. SHE) applied on the anode-region boundary (left face, x = 0).
    /// </param>
    /// <param name="cathodePotential">
    /// Dirichlet value (V vs. SHE) applied on the cathode-region boundary (right face, x = Lx).
    /// </param>
    /// <param name="ionicConductivity">Electrolyte conductivity κ (S/m).</param>
    /// <param name="anodeMaterial">Anode material for Faraday's-law corrosion-rate conversion.</param>
    /// <returns>
    /// A tuple of (nodalPotentials, nodalCorrosionRates), both flattened in
    /// row-major order (index = i*ny + j).
    /// </returns>
    internal static (double[] NodalPotentials, double[] NodalCorrosionRates) Solve(
        GeometryMesh                 mesh,
        double                       anodePotential,
        double                       cathodePotential,
        double                       ionicConductivity,
        IMaterial                    anodeMaterial,
        ICorrosionProductMaterial?   corrosionProductMaterial = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(anodeMaterial);

        int nx = mesh.NodesX;
        int ny = mesh.NodesY;

        double lx = mesh.XCoordinates[nx - 1] - mesh.XCoordinates[0];
        double ly = mesh.YCoordinates[ny - 1] - mesh.YCoordinates[0];

        // Guard against degenerate meshes.
        if (lx <= 0.0) lx = 1.0;
        if (ly <= 0.0) ly = 1.0;

        // BCs: anode potential on the left face, cathode potential on the right face,
        // zero-flux (insulating) on top and bottom.
        var leftBC   = new DirichletBC(anodePotential);
        var rightBC  = new DirichletBC(cathodePotential);
        var bottomBC = new NeumannBC(0.0);
        var topBC    = new NeumannBC(0.0);

        double[] conductivityMap = BuildConductivityMap(mesh, ionicConductivity, corrosionProductMaterial);

        var solver = new LaplaceSolver2D(
            nx, ny, lx, ly,
            leftBC, rightBC, bottomBC, topBC,
            conductivityMap: conductivityMap,
            omega: 1.5);

        var result = solver.Solve(new PdeSolverOptions { MaxIterations = 2000, Tolerance = 1e-8 });
        double[] phi = result.Solution;

        // Compute corrosion rates from current density j = -κ * dφ/dx (forward differences).
        double dx = lx / (nx - 1);
        double n  = anodeMaterial.ElectronsTransferred;
        double M  = anodeMaterial.MolarMass;
        double rho = anodeMaterial.Density;

        double[] nodalPotentials     = new double[nx * ny];
        double[] nodalCorrosionRates = new double[nx * ny];

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = i * ny + j;
                nodalPotentials[idx] = phi[idx];

                // Local current density (A/m²): central or forward difference in x.
                double dphiDx;
                if (i == 0)
                    dphiDx = (phi[1 * ny + j] - phi[0 * ny + j]) / dx;
                else if (i == nx - 1)
                    dphiDx = (phi[(nx - 1) * ny + j] - phi[(nx - 2) * ny + j]) / dx;
                else
                    dphiDx = (phi[(i + 1) * ny + j] - phi[(i - 1) * ny + j]) / (2.0 * dx);

                double currentDensity = -conductivityMap[idx] * dphiDx; // A/m²
                if (mesh.Regions[i, j] == NodePhase.CorrosionProduct)
                    currentDensity = CorrosionProductBehavior.ApplyBarrierResistance(currentDensity, corrosionProductMaterial);

                // Faraday's law: rate (mm/year) = |i| × M / (n × F × ρ) × seconds_per_year × 1000
                nodalCorrosionRates[idx] = Math.Abs(currentDensity) * M
                    / (n * PhysicalConstants.Faraday * rho)
                    * SecondsPerYear * 1000.0;
            }
        }

        return (nodalPotentials, nodalCorrosionRates);
    }

    private static double[] BuildConductivityMap(
        GeometryMesh               mesh,
        double                     ionicConductivity,
        ICorrosionProductMaterial? corrosionProductMaterial)
    {
        int nx = mesh.NodesX;
        int ny = mesh.NodesY;
        double electrolyteConductivity = ionicConductivity > 0.0
            ? ionicConductivity
            : DefaultElectrolyteConductivity;
        double corrosionProductConductivity = corrosionProductMaterial is null
            ? Math.Max(electrolyteConductivity * CorrosionProductConductivityRatio,
                       MinimumCorrosionProductConductivity)
            : CorrosionProductBehavior.GetEffectiveConductivity(corrosionProductMaterial);

        double[] conductivityMap = new double[nx * ny];
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = i * ny + j;
                conductivityMap[idx] = mesh.Regions[i, j] switch
                {
                    NodePhase.Anode or NodePhase.Cathode => MetalConductivity,
                    NodePhase.CorrosionProduct => corrosionProductConductivity,
                    _ => electrolyteConductivity,
                };
            }
        }

        return conductivityMap;
    }
}

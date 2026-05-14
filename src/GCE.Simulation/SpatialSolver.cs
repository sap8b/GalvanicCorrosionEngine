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
/// standard potentials and applied at the dynamically identified
/// metal-electrolyte interfaces (anode/electrolyte and cathode/electrolyte).
/// Nodes that are not part of those interfaces use zero-flux boundaries on the
/// outer domain perimeter.
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
    private const double SolverTolerance = 1e-8;
    private const int SolverMaxIterations = 2000;
    private const double SOROmega = 1.5;

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

        double[] conductivityMap = BuildConductivityMap(mesh, ionicConductivity, corrosionProductMaterial);
        (bool[] fixedNodeMask, double[] fixedNodeValues) = BuildDynamicBoundaryConditions(
            mesh, anodePotential, cathodePotential);
        double[] phi = SolvePotentialField(
            nx, ny, lx, ly, conductivityMap, fixedNodeMask, fixedNodeValues);

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

    private static (bool[] FixedNodeMask, double[] FixedNodeValues) BuildDynamicBoundaryConditions(
        GeometryMesh mesh,
        double       anodePotential,
        double       cathodePotential)
    {
        int nx = mesh.NodesX;
        int ny = mesh.NodesY;
        bool[] fixedNodeMask = new bool[nx * ny];
        double[] fixedNodeValues = new double[nx * ny];

        bool hasAnodeInterface = false;
        bool hasCathodeInterface = false;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = i * ny + j;
                if (mesh.Regions[i, j] == NodePhase.Anode
                    && HasNeighborPhase(mesh.Regions, nx, ny, i, j, NodePhase.Electrolyte))
                {
                    fixedNodeMask[idx] = true;
                    fixedNodeValues[idx] = anodePotential;
                    hasAnodeInterface = true;
                }
                else if (mesh.Regions[i, j] == NodePhase.Cathode
                         && HasNeighborPhase(mesh.Regions, nx, ny, i, j, NodePhase.Electrolyte))
                {
                    fixedNodeMask[idx] = true;
                    fixedNodeValues[idx] = cathodePotential;
                    hasCathodeInterface = true;
                }
            }
        }

        // Fallback to legacy face BCs when no interface nodes are available.
        if (!hasAnodeInterface)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = j;
                fixedNodeMask[idx] = true;
                fixedNodeValues[idx] = anodePotential;
            }
        }

        if (!hasCathodeInterface)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = (nx - 1) * ny + j;
                fixedNodeMask[idx] = true;
                fixedNodeValues[idx] = cathodePotential;
            }
        }

        return (fixedNodeMask, fixedNodeValues);
    }

    private static bool HasNeighborPhase(
        NodePhase[,] regions,
        int          nx,
        int          ny,
        int          i,
        int          j,
        NodePhase    targetPhase)
    {
        if (i > 0 && regions[i - 1, j] == targetPhase) return true;
        if (i < nx - 1 && regions[i + 1, j] == targetPhase) return true;
        if (j > 0 && regions[i, j - 1] == targetPhase) return true;
        if (j < ny - 1 && regions[i, j + 1] == targetPhase) return true;
        return false;
    }

    private static double[] SolvePotentialField(
        int      nx,
        int      ny,
        double   lx,
        double   ly,
        double[] conductivityMap,
        bool[]   fixedNodeMask,
        double[] fixedNodeValues)
    {
        double dx = nx > 1 ? lx / (nx - 1) : 1.0;
        double dy = ny > 1 ? ly / (ny - 1) : 1.0;
        double dx2 = dx * dx;
        double dy2 = dy * dy;

        var phi = new double[nx * ny];
        for (int idx = 0; idx < phi.Length; idx++)
            if (fixedNodeMask[idx])
                phi[idx] = fixedNodeValues[idx];

        for (int iter = 0; iter < SolverMaxIterations; iter++)
        {
            double residual = 0.0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    int idx = i * ny + j;
                    if (fixedNodeMask[idx])
                    {
                        phi[idx] = fixedNodeValues[idx];
                        continue;
                    }

                    // For outer-boundary nodes without fixed Dirichlet values, enforce
                    // zero normal flux by mirroring the interior neighbor as the ghost
                    // node value (∂phi/∂n = 0).
                    int westI = i > 0 ? i - 1 : (nx > 1 ? 1 : 0);
                    int eastI = i < nx - 1 ? i + 1 : (nx > 1 ? nx - 2 : 0);
                    int southJ = j > 0 ? j - 1 : (ny > 1 ? 1 : 0);
                    int northJ = j < ny - 1 ? j + 1 : (ny > 1 ? ny - 2 : 0);

                    double conductivity = conductivityMap[idx];
                    double kW = i > 0
                        ? FaceConductivity(conductivity, conductivityMap[westI * ny + j])
                        : conductivity;
                    double kE = i < nx - 1
                        ? FaceConductivity(conductivity, conductivityMap[eastI * ny + j])
                        : conductivity;
                    double kS = j > 0
                        ? FaceConductivity(conductivity, conductivityMap[i * ny + southJ])
                        : conductivity;
                    double kN = j < ny - 1
                        ? FaceConductivity(conductivity, conductivityMap[i * ny + northJ])
                        : conductivity;

                    double uW = phi[westI * ny + j];
                    double uE = phi[eastI * ny + j];
                    double uS = phi[i * ny + southJ];
                    double uN = phi[i * ny + northJ];

                    double denominator = (kW + kE) / dx2 + (kS + kN) / dy2;
                    if (denominator <= 0.0)
                        continue;

                    double uGaussSeidel = ((kW * uW + kE * uE) / dx2
                                         + (kS * uS + kN * uN) / dy2)
                                        / denominator;

                    double oldValue = phi[idx];
                    double newValue = (1.0 - SOROmega) * oldValue + SOROmega * uGaussSeidel;
                    phi[idx] = newValue;
                    double change = Math.Abs(newValue - oldValue);
                    if (change > residual)
                        residual = change;
                }
            }

            if (residual < SolverTolerance)
                break;
        }

        for (int idx = 0; idx < phi.Length; idx++)
            if (fixedNodeMask[idx])
                phi[idx] = fixedNodeValues[idx];

        return phi;
    }

    private static double FaceConductivity(double first, double second)
    {
        double sum = first + second;
        if (sum <= 0.0)
            throw new InvalidOperationException("Face conductivity requires positive conductivities.");
        return 2.0 * first * second / sum;
    }
}

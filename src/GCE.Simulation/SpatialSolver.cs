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
/// At dynamically identified metal-electrolyte interfaces the solver imposes
/// Robin (mixed) boundary conditions derived from the Butler–Volmer equation:
/// <code>κ ∂φ/∂n = i_BV(φ − E_eq)</code>
/// where <c>n</c> is the outward normal from the electrode into the electrolyte,
/// <c>E_eq</c> is the electrode equilibrium potential, and <c>i_BV</c> is the
/// full Butler–Volmer current density.  This couples the local electrolyte-phase
/// potential to the electrode kinetics without prescribing a fixed (Dirichlet)
/// potential.
/// </para>
/// <para>
/// When no electrode/electrolyte interface is detected for a given electrode type
/// (e.g. a mesh that contains only electrolyte nodes), the solver falls back to
/// legacy Dirichlet face boundary conditions on the domain perimeter.
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

    // Small perturbation used for the numerical Jacobian of the BV equation.
    private const double JacobianPerturbation = 1e-5;

    // Holds pre-computed Robin BC data for a single electrode interface node.
    private readonly record struct RobinInterfaceData(
        bool IsRobin,
        double EquilibriumPotential,
        IElectrodeKinetics? Kinetics,
        (int Idx, double Delta, double KFace)[]? Neighbors);

    /// <summary>
    /// Solves the variable-conductivity Laplace equation on <paramref name="mesh"/>
    /// and returns the nodal potential field and nodal corrosion rates.
    /// </summary>
    /// <param name="mesh">The spatial mesh to solve on.</param>
    /// <param name="anodeMaterial">
    /// Anode material, used for Butler–Volmer Robin BCs at the anode/electrolyte
    /// interface and for Faraday's-law corrosion-rate conversion.
    /// </param>
    /// <param name="cathodeMaterial">
    /// Cathode material, used for Butler–Volmer Robin BCs at the
    /// cathode/electrolyte interface.
    /// </param>
    /// <param name="ionicConductivity">Electrolyte conductivity κ (S/m).</param>
    /// <param name="corrosionProductMaterial">Optional corrosion-product barrier layer.</param>
    /// <param name="temperatureKelvin">
    /// Absolute temperature (K) used to evaluate the Butler–Volmer thermal factor F/RT.
    /// Defaults to 298.15 K (25 °C).
    /// </param>
    /// <returns>
    /// A tuple of (nodalPotentials, nodalCorrosionRates), both flattened in
    /// row-major order (index = i*ny + j).
    /// </returns>
    internal static (double[] NodalPotentials, double[] NodalCorrosionRates) Solve(
        GeometryMesh                 mesh,
        IMaterial                    anodeMaterial,
        IMaterial                    cathodeMaterial,
        double                       ionicConductivity,
        ICorrosionProductMaterial?   corrosionProductMaterial = null,
        double                       temperatureKelvin = 298.15)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(anodeMaterial);
        ArgumentNullException.ThrowIfNull(cathodeMaterial);

        int nx = mesh.NodesX;
        int ny = mesh.NodesY;

        double lx = mesh.XCoordinates[nx - 1] - mesh.XCoordinates[0];
        double ly = mesh.YCoordinates[ny - 1] - mesh.YCoordinates[0];

        // Guard against degenerate meshes.
        if (lx <= 0.0) lx = 1.0;
        if (ly <= 0.0) ly = 1.0;

        double dx = nx > 1 ? lx / (nx - 1) : 1.0;
        double dy = ny > 1 ? ly / (ny - 1) : 1.0;

        var anodeKinetics = new ButlerVolmerKinetics(
            anodeMaterial.ExchangeCurrentDensity,
            temperatureKelvin: temperatureKelvin);
        var cathodeKinetics = new ButlerVolmerKinetics(
            cathodeMaterial.ExchangeCurrentDensity,
            temperatureKelvin: temperatureKelvin);

        double[] conductivityMap = BuildConductivityMap(mesh, ionicConductivity, corrosionProductMaterial);

        (bool[] dirichletMask, double[] dirichletValues, RobinInterfaceData[] robinData) =
            BuildInterfaceConditions(
                mesh, nx, ny, dx, dy,
                anodeMaterial, cathodeMaterial,
                anodeKinetics, cathodeKinetics,
                conductivityMap);

        double[] phi = SolvePotentialField(
            nx, ny, lx, ly, conductivityMap, dirichletMask, dirichletValues, robinData);

        // Compute corrosion rates from current density j = -κ * dφ/dx (forward differences).
        double n   = anodeMaterial.ElectronsTransferred;
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

    // ── Interface condition builder ────────────────────────────────────────────

    /// <summary>
    /// Builds Dirichlet (fallback) and Robin (Butler-Volmer) interface data.
    /// Electrode nodes that adjoin an electrolyte node receive a Robin entry;
    /// if no such interface is found for an electrode type the corresponding
    /// domain-perimeter face falls back to a Dirichlet condition at E_eq.
    /// </summary>
    private static (bool[] DirichletMask, double[] DirichletValues, RobinInterfaceData[] RobinData)
        BuildInterfaceConditions(
            GeometryMesh     mesh,
            int              nx,
            int              ny,
            double           dx,
            double           dy,
            IMaterial        anodeMaterial,
            IMaterial        cathodeMaterial,
            IElectrodeKinetics anodeKinetics,
            IElectrodeKinetics cathodeKinetics,
            double[]         conductivityMap)
    {
        bool[]             dirichletMask   = new bool[nx * ny];
        double[]           dirichletValues = new double[nx * ny];
        RobinInterfaceData[] robinData       = new RobinInterfaceData[nx * ny];

        bool hasAnodeInterface   = false;
        bool hasCathodeInterface = false;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                int  idx    = i * ny + j;
                bool isAnode   = mesh.Regions[i, j] == NodePhase.Anode;
                bool isCathode = mesh.Regions[i, j] == NodePhase.Cathode;

                if (!isAnode && !isCathode)
                    continue;

                // Collect all adjacent electrolyte-phase nodes.
                var neighbors = FindElectrolyteNeighbors(
                    mesh, nx, ny, i, j, dx, dy, conductivityMap);

                if (neighbors.Count == 0)
                    continue;

                if (isAnode)
                {
                    robinData[idx] = new RobinInterfaceData(
                        IsRobin:              true,
                        EquilibriumPotential: anodeMaterial.StandardPotential,
                        Kinetics:             anodeKinetics,
                        Neighbors:            [.. neighbors]);
                    hasAnodeInterface = true;
                }
                else
                {
                    robinData[idx] = new RobinInterfaceData(
                        IsRobin:              true,
                        EquilibriumPotential: cathodeMaterial.StandardPotential,
                        Kinetics:             cathodeKinetics,
                        Neighbors:            [.. neighbors]);
                    hasCathodeInterface = true;
                }
            }
        }

        // Fallback to legacy Dirichlet face BCs when no interface was detected.
        if (!hasAnodeInterface)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = 0 * ny + j;
                dirichletMask[idx]   = true;
                dirichletValues[idx] = anodeMaterial.StandardPotential;
            }
        }

        if (!hasCathodeInterface)
        {
            for (int j = 0; j < ny; j++)
            {
                int idx = (nx - 1) * ny + j;
                dirichletMask[idx]   = true;
                dirichletValues[idx] = cathodeMaterial.StandardPotential;
            }
        }

        return (dirichletMask, dirichletValues, robinData);
    }

    /// <summary>
    /// Returns (index, grid-spacing, harmonic-mean face conductivity) for every
    /// electrolyte-phase node adjacent to node (i, j).
    /// </summary>
    private static List<(int Idx, double Delta, double KFace)> FindElectrolyteNeighbors(
        GeometryMesh mesh,
        int          nx,
        int          ny,
        int          i,
        int          j,
        double       dx,
        double       dy,
        double[]     conductivityMap)
    {
        var result = new List<(int, double, double)>(4);
        int idx    = i * ny + j;
        double kC  = conductivityMap[idx];

        if (i > 0     && mesh.Regions[i - 1, j] == NodePhase.Electrolyte)
            result.Add(((i - 1) * ny + j, dx, FaceConductivity(kC, conductivityMap[(i - 1) * ny + j])));
        if (i < nx - 1 && mesh.Regions[i + 1, j] == NodePhase.Electrolyte)
            result.Add(((i + 1) * ny + j, dx, FaceConductivity(kC, conductivityMap[(i + 1) * ny + j])));
        if (j > 0     && mesh.Regions[i, j - 1] == NodePhase.Electrolyte)
            result.Add((i * ny + (j - 1), dy, FaceConductivity(kC, conductivityMap[i * ny + (j - 1)])));
        if (j < ny - 1 && mesh.Regions[i, j + 1] == NodePhase.Electrolyte)
            result.Add((i * ny + (j + 1), dy, FaceConductivity(kC, conductivityMap[i * ny + (j + 1)])));

        return result;
    }

    // ── Potential solver ───────────────────────────────────────────────────────

    private static double[] SolvePotentialField(
        int                  nx,
        int                  ny,
        double               lx,
        double               ly,
        double[]             conductivityMap,
        bool[]               dirichletMask,
        double[]             dirichletValues,
        RobinInterfaceData[] robinData)
    {
        double dx  = nx > 1 ? lx / (nx - 1) : 1.0;
        double dy  = ny > 1 ? ly / (ny - 1) : 1.0;
        double dx2 = dx * dx;
        double dy2 = dy * dy;

        var phi = new double[nx * ny];

        // Seed Dirichlet and Robin nodes with their equilibrium/prescribed values
        // to give the iterative solver a physically meaningful starting point.
        for (int idx = 0; idx < phi.Length; idx++)
        {
            if (dirichletMask[idx])
                phi[idx] = dirichletValues[idx];
            else if (robinData[idx].IsRobin)
                phi[idx] = robinData[idx].EquilibriumPotential;
        }

        for (int iter = 0; iter < SolverMaxIterations; iter++)
        {
            double residual = 0.0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    int idx = i * ny + j;

                    if (dirichletMask[idx])
                    {
                        phi[idx] = dirichletValues[idx];
                        continue;
                    }

                    if (robinData[idx].IsRobin)
                    {
                        // Robin (Butler-Volmer) interface node: κ ∂φ/∂n = i_BV(φ − E_eq).
                        // Use a linearised Newton update averaged over all electrolyte-
                        // facing neighbours; plain Gauss-Seidel (ω = 1) for stability.
                        double oldValue = phi[idx];
                        double newValue = ComputeRobinUpdate(phi, idx, in robinData[idx]);
                        phi[idx] = newValue;
                        double change = Math.Abs(newValue - oldValue);
                        if (change > residual)
                            residual = change;
                        continue;
                    }

                    // Interior node: variable-conductivity Gauss-Seidel / SOR.
                    // For outer-boundary nodes without a prescribed value, enforce
                    // zero normal flux by mirroring the interior neighbour (ghost
                    // node value, ∂phi/∂n = 0).
                    int westI  = i > 0      ? i - 1 : (nx > 1 ? 1    : 0);
                    int eastI  = i < nx - 1 ? i + 1 : (nx > 1 ? nx-2 : 0);
                    int southJ = j > 0      ? j - 1 : (ny > 1 ? 1    : 0);
                    int northJ = j < ny - 1 ? j + 1 : (ny > 1 ? ny-2 : 0);

                    double conductivity = conductivityMap[idx];
                    double kW = i > 0
                        ? FaceConductivity(conductivity, conductivityMap[westI  * ny + j])
                        : conductivity;
                    double kE = i < nx - 1
                        ? FaceConductivity(conductivity, conductivityMap[eastI  * ny + j])
                        : conductivity;
                    double kS = j > 0
                        ? FaceConductivity(conductivity, conductivityMap[i * ny + southJ])
                        : conductivity;
                    double kN = j < ny - 1
                        ? FaceConductivity(conductivity, conductivityMap[i * ny + northJ])
                        : conductivity;

                    double uW = phi[westI  * ny + j];
                    double uE = phi[eastI  * ny + j];
                    double uS = phi[i * ny + southJ];
                    double uN = phi[i * ny + northJ];

                    double denominator = (kW + kE) / dx2 + (kS + kN) / dy2;
                    if (denominator <= 0.0)
                        continue;

                    double uGaussSeidel = ((kW * uW + kE * uE) / dx2
                                         + (kS * uS + kN * uN) / dy2)
                                        / denominator;

                    double oldVal = phi[idx];
                    double newVal = (1.0 - SOROmega) * oldVal + SOROmega * uGaussSeidel;
                    phi[idx] = newVal;
                    double delta = Math.Abs(newVal - oldVal);
                    if (delta > residual)
                        residual = delta;
                }
            }

            if (residual < SolverTolerance)
                break;
        }

        return phi;
    }

    /// <summary>
    /// Computes a linearised Robin (Butler-Volmer) update for an electrode
    /// interface node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each adjacent electrolyte neighbour d at distance Δ_d and harmonic-mean
    /// face conductivity κ_d, the Robin condition is:
    /// <code>κ_d · (φ_d − φ) / Δ_d = i_BV(φ − E_eq)</code>
    /// Linearising i_BV around the current iterate and solving for φ gives:
    /// <code>φ_new = (h_d · φ_d − i_BV_k + J_k · φ_k) / (h_d + J_k)</code>
    /// where h_d = κ_d/Δ_d, i_BV_k = i_BV(η_k), J_k = di_BV/dη|_{η_k},
    /// and η_k = φ_k − E_eq.  The result is averaged over all electrolyte
    /// neighbours.
    /// </para>
    /// </remarks>
    private static double ComputeRobinUpdate(
        double[]              phi,
        int                   idx,
        in RobinInterfaceData robin)
    {
        double phiCurrent = phi[idx];
        double eta  = phiCurrent - robin.EquilibriumPotential;
        double iBV  = robin.Kinetics!.CurrentDensity(eta);

        // Numerical Jacobian: di_BV/dη using a centred finite difference.
        double J = (robin.Kinetics.CurrentDensity(eta + JacobianPerturbation)
                  - robin.Kinetics.CurrentDensity(eta - JacobianPerturbation))
                 / (2.0 * JacobianPerturbation);

        double sum   = 0.0;
        int    count = robin.Neighbors!.Length;

        foreach ((int neighborIdx, double delta, double kFace) in robin.Neighbors)
        {
            double h     = kFace / delta;
            double denom = h + J;

            // denom = h + J; J ≥ 0 always, h > 0 always → denom > 0.
            if (denom <= 0.0)
            {
                sum += phiCurrent;
                continue;
            }

            sum += (h * phi[neighborIdx] - iBV + J * phiCurrent) / denom;
        }

        return count > 0 ? sum / count : phiCurrent;
    }

    private static double FaceConductivity(double first, double second)
    {
        double sum = first + second;
        if (sum <= 0.0)
            throw new InvalidOperationException("Face conductivity requires positive conductivities.");
        return 2.0 * first * second / sum;
    }
}

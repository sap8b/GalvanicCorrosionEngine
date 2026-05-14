namespace GCE.Configuration;

/// <summary>
/// Strategy for translating physical recession/deposition scales into mesh spacing.
/// </summary>
public enum NodeSpacingStrategy
{
    /// <summary>
    /// Use expected penetration/deposition depth and <see cref="NodeSpacingConfig.NodesPerExpectedDepth"/>
    /// to recommend a uniform node pitch.
    /// </summary>
    UniformFromExpectedDepth = 0,

    /// <summary>
    /// Use explicitly supplied <see cref="NodeSpacingConfig.XCoordinates"/> /
    /// <see cref="NodeSpacingConfig.YCoordinates"/> (supports non-uniform grids).
    /// </summary>
    ExplicitCoordinates = 1,
}

/// <summary>
/// Configuration for spatial node spacing and physical recession/deposition resolution.
/// </summary>
/// <remarks>
/// The minimum resolvable surface recession in this model is one node pitch (Δx).
/// For a notional 1 m² out-of-plane cross-section, "one node dissolved" corresponds to a
/// removed volume of Δx × 1 m².
/// </remarks>
public sealed class NodeSpacingConfig
{
    /// <summary>
    /// Gets or sets the spacing strategy. Default is <see cref="NodeSpacingStrategy.UniformFromExpectedDepth"/>.
    /// </summary>
    public NodeSpacingStrategy Strategy { get; set; } = NodeSpacingStrategy.UniformFromExpectedDepth;

    /// <summary>
    /// Gets or sets the expected maximum penetration depth (m).
    /// </summary>
    public double ExpectedPenetrationDepth { get; set; } = 1e-3;

    /// <summary>
    /// Gets or sets the expected maximum deposition thickness (m).
    /// </summary>
    public double ExpectedDepositionThickness { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the number of nodes used to resolve the larger of expected
    /// penetration or deposition depth. Must be positive.
    /// </summary>
    public int NodesPerExpectedDepth { get; set; } = 4;

    /// <summary>
    /// Gets or sets explicit x-coordinates (m) for mesh generation, when
    /// <see cref="Strategy"/> is <see cref="NodeSpacingStrategy.ExplicitCoordinates"/>.
    /// </summary>
    public double[]? XCoordinates { get; set; }

    /// <summary>
    /// Gets or sets explicit y-coordinates (m) for mesh generation, when
    /// <see cref="Strategy"/> is <see cref="NodeSpacingStrategy.ExplicitCoordinates"/>.
    /// </summary>
    public double[]? YCoordinates { get; set; }

    /// <summary>
    /// Gets the target depth scale (m) used for pitch selection:
    /// max(expected penetration, expected deposition).
    /// </summary>
    public double CharacteristicDepth =>
        Math.Max(ExpectedPenetrationDepth, ExpectedDepositionThickness);

    /// <summary>
    /// Gets the recommended uniform node pitch (m): CharacteristicDepth / NodesPerExpectedDepth.
    /// Returns 0 when <see cref="CharacteristicDepth"/> is 0.
    /// </summary>
    public double RecommendedNodePitch =>
        CharacteristicDepth <= 0.0 ? 0.0 : CharacteristicDepth / NodesPerExpectedDepth;
}

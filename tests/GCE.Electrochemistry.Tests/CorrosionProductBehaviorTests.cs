using GCE.Core;

namespace GCE.Electrochemistry.Tests;

public class CorrosionProductBehaviorTests
{
    private static readonly ICorrosionProductMaterial ZincOxide = CorrosionProductBehavior.ZincOxide;

    [Fact]
    public void BuiltIns_ExposeExpectedExamples()
    {
        Assert.Equal("Fe(OH)2", CorrosionProductBehavior.FerrousHydroxide.Name);
        Assert.Equal("ZnO", CorrosionProductBehavior.ZincOxide.Name);
        Assert.Equal("Al2O3", CorrosionProductBehavior.AluminiumOxide.Name);
    }

    [Fact]
    public void GetEffectiveConductivity_ReturnsMaterialConductivity()
    {
        Assert.Equal(ZincOxide.IonicConductivity,
            CorrosionProductBehavior.GetEffectiveConductivity(ZincOxide),
            precision: 12);
    }

    [Fact]
    public void ApplyBarrierResistance_ReducesCurrentDensity()
    {
        double adjusted = CorrosionProductBehavior.ApplyBarrierResistance(8.0, ZincOxide);
        Assert.Equal(1.0, adjusted, precision: 12);
    }

    [Fact]
    public void ComputeSolidVolume_UsesMolarVolume()
    {
        double volume = CorrosionProductBehavior.ComputeSolidVolume(2.0, ZincOxide);
        Assert.Equal(2.0 * ZincOxide.MolarVolume, volume, precision: 12);
    }

    [Fact]
    public void ComputeSaturationIndex_UsesSolubilityProduct()
    {
        double omega = CorrosionProductBehavior.ComputeSaturationIndex(6.0e-17, ZincOxide);
        Assert.Equal(2.0, omega, precision: 12);
    }

    [Fact]
    public void ShouldRedissolve_IsTrue_WhenUndersaturated()
    {
        Assert.True(CorrosionProductBehavior.ShouldRedissolve(1.0e-17, ZincOxide));
        Assert.False(CorrosionProductBehavior.ShouldRedissolve(6.0e-17, ZincOxide));
    }
}

using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Electrochemistry.Tests;

// ── ButlerVolmerKinetics tests ────────────────────────────────────────────────

public class ButlerVolmerKineticsTests
{
    private const double I0 = 1e-3;       // exchange current density (A/m²)
    private const double Alpha = 0.5;     // symmetric transfer coefficients
    private const double T = 298.15;      // room temperature (K)

    private static ButlerVolmerKinetics MakeSymmetric(double? limitingCurrent = null) =>
        new(I0, Alpha, Alpha, T, limitingCurrent);

    // ── IElectrodeKinetics properties ─────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_Properties_StoredCorrectly()
    {
        var bv = new ButlerVolmerKinetics(2e-4, 0.4, 0.6, 310.0, 5.0);
        Assert.Equal(2e-4, bv.ExchangeCurrentDensity);
        Assert.Equal(0.4, bv.AnodicTransferCoefficient);
        Assert.Equal(0.6, bv.CathodicTransferCoefficient);
        Assert.Equal(310.0, bv.TemperatureKelvin);
        Assert.Equal(5.0, bv.LimitingCurrentDensity);
    }

    [Fact]
    public void ButlerVolmerKinetics_ImplementsIElectrodeKinetics()
    {
        IElectrodeKinetics kinetics = MakeSymmetric();
        Assert.Equal(I0, kinetics.ExchangeCurrentDensity);
    }

    // ── Current density at zero overpotential ─────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_CurrentDensity_IsZeroAtZeroOverpotential()
    {
        var bv = MakeSymmetric();
        Assert.Equal(0.0, bv.CurrentDensity(0.0), precision: 15);
    }

    // ── Sign conventions ──────────────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_CurrentDensity_IsPositiveForPositiveOverpotential()
    {
        var bv = MakeSymmetric();
        Assert.True(bv.CurrentDensity(0.1) > 0.0);
    }

    [Fact]
    public void ButlerVolmerKinetics_CurrentDensity_IsNegativeForNegativeOverpotential()
    {
        var bv = MakeSymmetric();
        Assert.True(bv.CurrentDensity(-0.1) < 0.0);
    }

    // ── Anodic / cathodic branches ────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_AnodicCurrentDensity_IsAlwaysPositive()
    {
        var bv = MakeSymmetric();
        Assert.True(bv.AnodicCurrentDensity(-0.5) > 0.0);
        Assert.True(bv.AnodicCurrentDensity(0.0) > 0.0);
        Assert.True(bv.AnodicCurrentDensity(0.5) > 0.0);
    }

    [Fact]
    public void ButlerVolmerKinetics_CathodicCurrentDensity_IsAlwaysNonPositive()
    {
        var bv = MakeSymmetric();
        Assert.True(bv.CathodicCurrentDensity(-0.5) < 0.0);
        Assert.True(bv.CathodicCurrentDensity(0.0) < 0.0);
        Assert.True(bv.CathodicCurrentDensity(0.5) < 0.0);
    }

    [Fact]
    public void ButlerVolmerKinetics_AnodicAndCathodicBranches_SumToNetCurrent()
    {
        var bv = MakeSymmetric();
        double eta = 0.2;
        double net = bv.CurrentDensity(eta);
        double sum = bv.AnodicCurrentDensity(eta) + bv.CathodicCurrentDensity(eta);
        Assert.Equal(net, sum, precision: 12);
    }

    [Fact]
    public void ButlerVolmerKinetics_AnodicBranch_AtZeroOverpotential_EqualsExchangeCurrent()
    {
        var bv = MakeSymmetric();
        Assert.Equal(I0, bv.AnodicCurrentDensity(0.0), precision: 12);
    }

    [Fact]
    public void ButlerVolmerKinetics_CathodicBranch_AtZeroOverpotential_IsNegativeExchangeCurrent()
    {
        var bv = MakeSymmetric();
        Assert.Equal(-I0, bv.CathodicCurrentDensity(0.0), precision: 12);
    }

    // ── Tafel slope verification ───────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_AnodicTafelSlope_IsCorrect()
    {
        // For large positive η the anodic branch dominates:
        // ln(i/i₀) = α_a * F * η / (R * T)
        // → slope of ln(i) vs. η is α_a * F / (R * T)
        var bv = MakeSymmetric();
        double eta1 = 0.3;
        double eta2 = 0.4;
        double slope = (Math.Log(bv.AnodicCurrentDensity(eta2)) - Math.Log(bv.AnodicCurrentDensity(eta1)))
                       / (eta2 - eta1);
        double expected = Alpha * PhysicalConstants.Faraday / (PhysicalConstants.GasConstant * T);
        Assert.Equal(expected, slope, precision: 6);
    }

    [Fact]
    public void ButlerVolmerKinetics_AsymmetricCoefficients_ProduceCorrectBranches()
    {
        double alphaA = 0.3;
        double alphaC = 0.7;
        var bv = new ButlerVolmerKinetics(I0, alphaA, alphaC, T);
        double factor = PhysicalConstants.Faraday / (PhysicalConstants.GasConstant * T);
        double eta = 0.2;

        double expectedAnodic = I0 * Math.Exp(alphaA * factor * eta);
        double expectedCathodic = -I0 * Math.Exp(-alphaC * factor * eta);

        Assert.Equal(expectedAnodic, bv.AnodicCurrentDensity(eta), precision: 10);
        Assert.Equal(expectedCathodic, bv.CathodicCurrentDensity(eta), precision: 10);
    }

    // ── Limiting current density ───────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_WithLimitingCurrent_CurrentIsBoundedByLimitingValue()
    {
        double iLim = 0.01; // 10 mA/m²
        var bv = MakeSymmetric(iLim);

        // At very large overpotential the current must saturate below i_lim
        double iLargeAnodic = bv.CurrentDensity(10.0);
        double iLargeCathodic = bv.CurrentDensity(-10.0);

        Assert.True(iLargeAnodic <= iLim, $"Anodic current {iLargeAnodic} should be ≤ iLim {iLim}");
        Assert.True(iLargeCathodic >= -iLim, $"Cathodic current {iLargeCathodic} should be ≥ -iLim {-iLim}");
    }

    [Fact]
    public void ButlerVolmerKinetics_WithLimitingCurrent_SmallOverpotential_UnaffectedByLimiting()
    {
        double iLim = 1000.0; // very large: should not affect small-η response
        var bvLimited = MakeSymmetric(iLim);
        var bvUnlimited = MakeSymmetric();

        double eta = 0.05;
        double limited = bvLimited.CurrentDensity(eta);
        double unlimited = bvUnlimited.CurrentDensity(eta);

        // When i_BV << i_lim the correction is negligible
        Assert.Equal(unlimited, limited, precision: 3);
    }

    [Fact]
    public void ButlerVolmerKinetics_WithoutLimitingCurrent_LimitingCurrentDensityIsNull()
    {
        var bv = MakeSymmetric();
        Assert.Null(bv.LimitingCurrentDensity);
    }

    // ── Default parameter values ───────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_DefaultParameters_AreSymmetricAtRoomTemperature()
    {
        var bv = new ButlerVolmerKinetics(I0);
        Assert.Equal(0.5, bv.AnodicTransferCoefficient);
        Assert.Equal(0.5, bv.CathodicTransferCoefficient);
        Assert.Equal(298.15, bv.TemperatureKelvin);
        Assert.Null(bv.LimitingCurrentDensity);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void ButlerVolmerKinetics_Constructor_ThrowsOnNonPositiveExchangeCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(-1e-3));
    }

    [Fact]
    public void ButlerVolmerKinetics_Constructor_ThrowsOnOutOfRangeTransferCoefficients()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, anodicTransferCoefficient: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, anodicTransferCoefficient: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, cathodicTransferCoefficient: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, cathodicTransferCoefficient: 1.1));
    }

    [Fact]
    public void ButlerVolmerKinetics_Constructor_ThrowsOnNonPositiveTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, temperatureKelvin: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, temperatureKelvin: -10.0));
    }

    [Fact]
    public void ButlerVolmerKinetics_Constructor_ThrowsOnNonPositiveLimitingCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, limitingCurrentDensity: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButlerVolmerKinetics(I0, limitingCurrentDensity: -1.0));
    }
}

// ── TafelKinetics tests ───────────────────────────────────────────────────────

public class TafelKineticsTests
{
    private const double I0 = 1e-3;
    private const double Alpha = 0.5;
    private const double T = 298.15;

    private static TafelKinetics MakeSymmetric() => new(I0, Alpha, Alpha, T);

    // ── IElectrodeKinetics properties ─────────────────────────────────────────

    [Fact]
    public void TafelKinetics_ImplementsIElectrodeKinetics()
    {
        IElectrodeKinetics kinetics = MakeSymmetric();
        Assert.Equal(I0, kinetics.ExchangeCurrentDensity);
    }

    [Fact]
    public void TafelKinetics_Properties_StoredCorrectly()
    {
        var t = new TafelKinetics(2e-4, 0.4, 0.6, 310.0);
        Assert.Equal(2e-4, t.ExchangeCurrentDensity);
        Assert.Equal(0.4, t.AnodicTransferCoefficient);
        Assert.Equal(0.6, t.CathodicTransferCoefficient);
        Assert.Equal(310.0, t.TemperatureKelvin);
    }

    // ── Current density at zero overpotential ─────────────────────────────────

    [Fact]
    public void TafelKinetics_CurrentDensity_IsZeroAtZeroOverpotential()
    {
        var tafel = MakeSymmetric();
        Assert.Equal(0.0, tafel.CurrentDensity(0.0));
    }

    // ── Sign conventions ──────────────────────────────────────────────────────

    [Fact]
    public void TafelKinetics_CurrentDensity_IsPositiveForPositiveOverpotential()
    {
        var tafel = MakeSymmetric();
        Assert.True(tafel.CurrentDensity(0.1) > 0.0);
    }

    [Fact]
    public void TafelKinetics_CurrentDensity_IsNegativeForNegativeOverpotential()
    {
        var tafel = MakeSymmetric();
        Assert.True(tafel.CurrentDensity(-0.1) < 0.0);
    }

    // ── Anodic / cathodic branches ────────────────────────────────────────────

    [Fact]
    public void TafelKinetics_AnodicCurrentDensity_IsAlwaysPositive()
    {
        var tafel = MakeSymmetric();
        Assert.True(tafel.AnodicCurrentDensity(-0.5) > 0.0);
        Assert.True(tafel.AnodicCurrentDensity(0.0) > 0.0);
        Assert.True(tafel.AnodicCurrentDensity(0.5) > 0.0);
    }

    [Fact]
    public void TafelKinetics_CathodicCurrentDensity_IsAlwaysNonPositive()
    {
        var tafel = MakeSymmetric();
        Assert.True(tafel.CathodicCurrentDensity(-0.5) < 0.0);
        Assert.True(tafel.CathodicCurrentDensity(0.0) < 0.0);
        Assert.True(tafel.CathodicCurrentDensity(0.5) < 0.0);
    }

    // ── Tafel approximation: routes to correct branch ─────────────────────────

    [Fact]
    public void TafelKinetics_PositiveOverpotential_UsesAnodicBranch()
    {
        var tafel = MakeSymmetric();
        double eta = 0.3;
        Assert.Equal(tafel.AnodicCurrentDensity(eta), tafel.CurrentDensity(eta), precision: 15);
    }

    [Fact]
    public void TafelKinetics_NegativeOverpotential_UsesCathodicBranch()
    {
        var tafel = MakeSymmetric();
        double eta = -0.3;
        Assert.Equal(tafel.CathodicCurrentDensity(eta), tafel.CurrentDensity(eta), precision: 15);
    }

    // ── Agreement with ButlerVolmer at large overpotentials ───────────────────

    [Fact]
    public void TafelKinetics_AtLargePositiveOverpotential_ApproximatesButlerVolmer()
    {
        var tafel = MakeSymmetric();
        var bv = new ButlerVolmerKinetics(I0, Alpha, Alpha, T);

        // At η = 0.5 V the back-reaction is very small; Tafel should be close to BV
        double eta = 0.5;
        double tafelI = tafel.CurrentDensity(eta);
        double bvI = bv.CurrentDensity(eta);
        double relError = Math.Abs(tafelI - bvI) / Math.Abs(bvI);
        Assert.True(relError < 1e-6, $"Relative error {relError} too large at η={eta}");
    }

    [Fact]
    public void TafelKinetics_AtLargeNegativeOverpotential_ApproximatesButlerVolmer()
    {
        var tafel = MakeSymmetric();
        var bv = new ButlerVolmerKinetics(I0, Alpha, Alpha, T);

        double eta = -0.5;
        double tafelI = tafel.CurrentDensity(eta);
        double bvI = bv.CurrentDensity(eta);
        double relError = Math.Abs(tafelI - bvI) / Math.Abs(bvI);
        Assert.True(relError < 1e-6, $"Relative error {relError} too large at η={eta}");
    }

    // ── Default parameter values ───────────────────────────────────────────────

    [Fact]
    public void TafelKinetics_DefaultParameters_AreSymmetricAtRoomTemperature()
    {
        var tafel = new TafelKinetics(I0);
        Assert.Equal(0.5, tafel.AnodicTransferCoefficient);
        Assert.Equal(0.5, tafel.CathodicTransferCoefficient);
        Assert.Equal(298.15, tafel.TemperatureKelvin);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void TafelKinetics_Constructor_ThrowsOnNonPositiveExchangeCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(-1e-3));
    }

    [Fact]
    public void TafelKinetics_Constructor_ThrowsOnOutOfRangeTransferCoefficients()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, anodicTransferCoefficient: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, anodicTransferCoefficient: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, cathodicTransferCoefficient: -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, cathodicTransferCoefficient: 1.1));
    }

    [Fact]
    public void TafelKinetics_Constructor_ThrowsOnNonPositiveTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, temperatureKelvin: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TafelKinetics(I0, temperatureKelvin: -10.0));
    }
}

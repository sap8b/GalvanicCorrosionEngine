using GCE.Numerics.Solvers;

namespace GCE.Numerics.Tests;

// ── BoundaryConditionType ─────────────────────────────────────────────────────

public class BoundaryConditionTypeTests
{
    [Fact]
    public void BoundaryConditionType_HasThreeMembers()
    {
        var values = Enum.GetValues<BoundaryConditionType>();
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void BoundaryConditionType_ContainsDirichlet()
    {
        Assert.True(Enum.IsDefined(BoundaryConditionType.Dirichlet));
    }

    [Fact]
    public void BoundaryConditionType_ContainsNeumann()
    {
        Assert.True(Enum.IsDefined(BoundaryConditionType.Neumann));
    }

    [Fact]
    public void BoundaryConditionType_ContainsRobin()
    {
        Assert.True(Enum.IsDefined(BoundaryConditionType.Robin));
    }
}

// ── DirichletBC ───────────────────────────────────────────────────────────────

public class DirichletBCTests
{
    [Fact]
    public void DirichletBC_Type_IsDirichlet()
    {
        var bc = new DirichletBC(1.0);
        Assert.Equal(BoundaryConditionType.Dirichlet, bc.Type);
    }

    [Fact]
    public void DirichletBC_ConstantConstructor_Evaluate_ReturnsConstant()
    {
        var bc = new DirichletBC(5.0);
        Assert.Equal(5.0, bc.Evaluate(0.0));
        Assert.Equal(5.0, bc.Evaluate(10.0));
        Assert.Equal(5.0, bc.Evaluate(100.0));
    }

    [Fact]
    public void DirichletBC_FuncConstructor_Evaluate_InvokesFunc()
    {
        // g(t) = 2t + 1
        var bc = new DirichletBC(t => 2.0 * t + 1.0);
        Assert.Equal(1.0, bc.Evaluate(0.0),  precision: 12);
        Assert.Equal(3.0, bc.Evaluate(1.0),  precision: 12);
        Assert.Equal(11.0, bc.Evaluate(5.0), precision: 12);
    }

    [Fact]
    public void DirichletBC_FuncConstructor_NullFunc_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DirichletBC(null!));
    }

    [Fact]
    public void DirichletBC_ImplementsIBoundaryCondition()
    {
        IBoundaryCondition bc = new DirichletBC(0.0);
        Assert.NotNull(bc);
        Assert.Equal(BoundaryConditionType.Dirichlet, bc.Type);
    }

    [Fact]
    public void DirichletBC_TimeVarying_CanModelSteadyStateAtTimeZero()
    {
        // Sinusoidal BC: g(t) = sin(t)
        var bc = new DirichletBC(t => Math.Sin(t));
        Assert.Equal(0.0,           bc.Evaluate(0.0),                   precision: 12);
        Assert.Equal(Math.Sin(1.0), bc.Evaluate(1.0),                   precision: 12);
        Assert.Equal(1.0,           bc.Evaluate(Math.PI / 2.0),         precision: 12);
    }
}

// ── NeumannBC ─────────────────────────────────────────────────────────────────

public class NeumannBCTests
{
    [Fact]
    public void NeumannBC_Type_IsNeumann()
    {
        var bc = new NeumannBC(0.0);
        Assert.Equal(BoundaryConditionType.Neumann, bc.Type);
    }

    [Fact]
    public void NeumannBC_ConstantConstructor_Evaluate_ReturnsConstant()
    {
        var bc = new NeumannBC(3.5);
        Assert.Equal(3.5, bc.Evaluate(0.0));
        Assert.Equal(3.5, bc.Evaluate(50.0));
    }

    [Fact]
    public void NeumannBC_ZeroFlux_ModelsInsulatingBoundary()
    {
        var bc = new NeumannBC(0.0);
        Assert.Equal(0.0, bc.Evaluate(0.0));
        Assert.Equal(0.0, bc.Evaluate(999.0));
    }

    [Fact]
    public void NeumannBC_FuncConstructor_Evaluate_InvokesFunc()
    {
        // q(t) = -t
        var bc = new NeumannBC(t => -t);
        Assert.Equal(0.0,   bc.Evaluate(0.0),  precision: 12);
        Assert.Equal(-2.5,  bc.Evaluate(2.5),  precision: 12);
        Assert.Equal(-10.0, bc.Evaluate(10.0), precision: 12);
    }

    [Fact]
    public void NeumannBC_FuncConstructor_NullFunc_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NeumannBC(null!));
    }

    [Fact]
    public void NeumannBC_ImplementsIBoundaryCondition()
    {
        IBoundaryCondition bc = new NeumannBC(1.0);
        Assert.NotNull(bc);
        Assert.Equal(BoundaryConditionType.Neumann, bc.Type);
    }
}

// ── RobinBC ───────────────────────────────────────────────────────────────────

public class RobinBCTests
{
    [Fact]
    public void RobinBC_Type_IsRobin()
    {
        var bc = new RobinBC(1.0, 1.0, 0.0);
        Assert.Equal(BoundaryConditionType.Robin, bc.Type);
    }

    [Fact]
    public void RobinBC_ConstantConstructor_StoresAlphaAndBeta()
    {
        var bc = new RobinBC(2.0, 3.0, 6.0);
        Assert.Equal(2.0, bc.Alpha);
        Assert.Equal(3.0, bc.Beta);
    }

    [Fact]
    public void RobinBC_ConstantConstructor_Evaluate_ReturnsConstantGamma()
    {
        var bc = new RobinBC(1.0, 1.0, 7.0);
        Assert.Equal(7.0, bc.Evaluate(0.0));
        Assert.Equal(7.0, bc.Evaluate(100.0));
    }

    [Fact]
    public void RobinBC_FuncConstructor_Evaluate_InvokesGammaFunc()
    {
        // γ(t) = t²
        var bc = new RobinBC(0.5, 2.0, t => t * t);
        Assert.Equal(0.0,  bc.Evaluate(0.0),  precision: 12);
        Assert.Equal(1.0,  bc.Evaluate(1.0),  precision: 12);
        Assert.Equal(4.0,  bc.Evaluate(2.0),  precision: 12);
        Assert.Equal(25.0, bc.Evaluate(5.0),  precision: 12);
    }

    [Fact]
    public void RobinBC_FuncConstructor_NullGammaFunc_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RobinBC(1.0, 1.0, (Func<double, double>)null!));
    }

    [Fact]
    public void RobinBC_ImplementsIBoundaryCondition()
    {
        IBoundaryCondition bc = new RobinBC(1.0, 0.0, 5.0);
        Assert.NotNull(bc);
        Assert.Equal(BoundaryConditionType.Robin, bc.Type);
    }

    [Fact]
    public void RobinBC_WithBetaZero_ReducesToDirichletLike()
    {
        // α·u + 0·(∂u/∂n) = γ  ⟹  u = γ/α at the boundary
        var bc = new RobinBC(alpha: 2.0, beta: 0.0, gamma: 10.0);
        Assert.Equal(0.0, bc.Beta);
        Assert.Equal(10.0, bc.Evaluate(0.0));   // prescribed value is γ = 10
    }

    [Fact]
    public void RobinBC_WithAlphaZero_ReducesToNeumannLike()
    {
        // 0·u + β·(∂u/∂n) = γ  ⟹  ∂u/∂n = γ/β at the boundary
        var bc = new RobinBC(alpha: 0.0, beta: 3.0, gamma: 9.0);
        Assert.Equal(0.0, bc.Alpha);
        Assert.Equal(9.0, bc.Evaluate(0.0));    // prescribed flux is γ = 9
    }
}

// ── IBoundaryCondition (polymorphic contract) ─────────────────────────────────

public class IBoundaryConditionContractTests
{
    [Fact]
    public void IBoundaryCondition_CanHoldAllThreeTypes()
    {
        IBoundaryCondition[] bcs =
        [
            new DirichletBC(1.0),
            new NeumannBC(0.0),
            new RobinBC(1.0, 1.0, 2.0),
        ];

        Assert.Equal(BoundaryConditionType.Dirichlet, bcs[0].Type);
        Assert.Equal(BoundaryConditionType.Neumann,   bcs[1].Type);
        Assert.Equal(BoundaryConditionType.Robin,     bcs[2].Type);
    }

    [Fact]
    public void IBoundaryCondition_Evaluate_WorksPolymorphically()
    {
        IBoundaryCondition[] bcs =
        [
            new DirichletBC(3.0),
            new NeumannBC(4.0),
            new RobinBC(1.0, 1.0, 5.0),
        ];

        Assert.Equal(3.0, bcs[0].Evaluate(0.0));
        Assert.Equal(4.0, bcs[1].Evaluate(0.0));
        Assert.Equal(5.0, bcs[2].Evaluate(0.0));
    }

    [Fact]
    public void IBoundaryCondition_TimeVarying_EvaluateDifferentTimes()
    {
        IBoundaryCondition dirichlet = new DirichletBC(t => t * 2.0);
        IBoundaryCondition neumann   = new NeumannBC(t => t + 1.0);

        Assert.Equal(0.0, dirichlet.Evaluate(0.0),  precision: 12);
        Assert.Equal(6.0, dirichlet.Evaluate(3.0),  precision: 12);
        Assert.Equal(1.0, neumann.Evaluate(0.0),    precision: 12);
        Assert.Equal(4.0, neumann.Evaluate(3.0),    precision: 12);
    }

    [Fact]
    public void IBoundaryCondition_AllTypes_EvaluateAtSteadyStateTime()
    {
        // Steady-state: time = 0 should always return the static value
        IBoundaryCondition dirichlet = new DirichletBC(42.0);
        IBoundaryCondition neumann   = new NeumannBC(-1.5);
        IBoundaryCondition robin     = new RobinBC(2.0, 1.0, 8.0);

        Assert.Equal(42.0, dirichlet.Evaluate(0.0));
        Assert.Equal(-1.5, neumann.Evaluate(0.0));
        Assert.Equal(8.0,  robin.Evaluate(0.0));
    }
}

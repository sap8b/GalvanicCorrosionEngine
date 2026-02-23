namespace GCE.Numerics;

/// <summary>
/// Represents a function f(t, y) used in ODE solvers.
/// </summary>
public delegate double OdeFunction(double t, double y);

/// <summary>
/// Solves a scalar ordinary differential equation dy/dt = f(t,y)
/// using the classic 4th-order Runge–Kutta method.
/// </summary>
public sealed class RungeKuttaSolver
{
    private readonly OdeFunction _f;

    /// <param name="f">The ODE right-hand-side function f(t, y).</param>
    public RungeKuttaSolver(OdeFunction f) => _f = f;

    /// <summary>
    /// Integrates one step of size <paramref name="h"/> from (t, y).
    /// </summary>
    public double Step(double t, double y, double h)
    {
        double k1 = _f(t, y);
        double k2 = _f(t + h / 2.0, y + h * k1 / 2.0);
        double k3 = _f(t + h / 2.0, y + h * k2 / 2.0);
        double k4 = _f(t + h, y + h * k3);
        return y + h * (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0;
    }

    /// <summary>
    /// Integrates the ODE from <paramref name="t0"/> to <paramref name="tEnd"/>
    /// using <paramref name="steps"/> equal steps, starting from <paramref name="y0"/>.
    /// </summary>
    public IReadOnlyList<(double T, double Y)> Integrate(
        double t0, double tEnd, double y0, int steps)
    {
        if (steps <= 0)
            throw new ArgumentOutOfRangeException(nameof(steps), "Must be positive.");

        double h = (tEnd - t0) / steps;
        var results = new List<(double, double)>(steps + 1) { (t0, y0) };

        double t = t0;
        double y = y0;
        for (int i = 0; i < steps; i++)
        {
            y = Step(t, y, h);
            t += h;
            results.Add((t, y));
        }

        return results;
    }
}

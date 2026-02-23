namespace GCE.Numerics;

/// <summary>
/// Finds a root of a scalar function using Brent's method.
/// </summary>
public static class BrentSolver
{
    private const int MaxIterations = 100;

    /// <summary>
    /// Finds x in [<paramref name="a"/>, <paramref name="b"/>] such that f(x) ≈ 0.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the method fails to converge within <see cref="MaxIterations"/> iterations.
    /// </exception>
    public static double FindRoot(
        Func<double, double> f,
        double a,
        double b,
        double tolerance = 1e-10)
    {
        double fa = f(a);
        double fb = f(b);

        if (fa * fb > 0)
            throw new ArgumentException("f(a) and f(b) must have opposite signs.");

        double c = a, fc = fa;
        double s = 0, fs = 0;
        bool mflag = true;
        double d = 0;

        for (int i = 0; i < MaxIterations; i++)
        {
            if (Math.Abs(fb) < tolerance) return b;
            if (Math.Abs(fa - fc) > tolerance && Math.Abs(fb - fc) > tolerance)
            {
                // Inverse quadratic interpolation
                s = a * fb * fc / ((fa - fb) * (fa - fc))
                  + b * fa * fc / ((fb - fa) * (fb - fc))
                  + c * fa * fb / ((fc - fa) * (fc - fb));
            }
            else
            {
                s = b - fb * (b - a) / (fb - fa);
            }

            bool condition1 = s < (3 * a + b) / 4 || s > b;
            bool condition2 = mflag && Math.Abs(s - b) >= Math.Abs(b - c) / 2;
            bool condition3 = !mflag && Math.Abs(s - b) >= Math.Abs(c - d) / 2;
            bool condition4 = mflag && Math.Abs(b - c) < tolerance;
            bool condition5 = !mflag && Math.Abs(c - d) < tolerance;

            if (condition1 || condition2 || condition3 || condition4 || condition5)
            {
                s = (a + b) / 2;
                mflag = true;
            }
            else
            {
                mflag = false;
            }

            fs = f(s);
            d = c;
            c = b;
            fc = fb;

            if (fa * fs < 0) { b = s; fb = fs; }
            else { a = s; fa = fs; }

            if (Math.Abs(fa) < Math.Abs(fb)) { (a, b) = (b, a); (fa, fb) = (fb, fa); }
        }

        throw new InvalidOperationException("Brent solver did not converge.");
    }
}

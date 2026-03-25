namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// A real-valued n × n tridiagonal matrix stored as three bands.
/// </summary>
/// <remarks>
/// <para>
/// Storage is O(n): only the sub-diagonal (a), main diagonal (b), and
/// super-diagonal (c) bands are kept.
/// </para>
/// <para>
/// The key advantage over a general dense or sparse matrix is the
/// O(n) Thomas algorithm for solving the system Ax = rhs
/// (see <see cref="Solve"/>).
/// </para>
/// <para>
/// Ported and generalised from <c>TridiagonalMatrix</c> in
/// <c>Diffusion2D_Library</c>.
/// </para>
/// </remarks>
public sealed class TridiagonalMatrix : IMatrix, IEquatable<TridiagonalMatrix>
{
    // Bands (all length n; lower[0] and upper[n-1] are unused sentinels)
    private readonly double[] _lower; // sub-diagonal:   a[1] … a[n-1]
    private readonly double[] _main;  // main diagonal:  b[0] … b[n-1]
    private readonly double[] _upper; // super-diagonal: c[0] … c[n-2]

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialises a zero tridiagonal matrix of order <paramref name="n"/>.</summary>
    /// <param name="n">Order (number of rows = number of columns).  Must be > 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="n"/> is ≤ 0.
    /// </exception>
    public TridiagonalMatrix(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        Size   = n;
        _lower = new double[n];
        _main  = new double[n];
        _upper = new double[n];
    }

    /// <summary>
    /// Initialises a tridiagonal matrix from three band vectors.
    /// </summary>
    /// <param name="subDiagonal">
    /// Sub-diagonal band of length <c>n − 1</c>
    /// (element <c>[i]</c> occupies row <c>i + 1</c>, column <c>i</c>).
    /// </param>
    /// <param name="mainDiagonal">Main diagonal of length <c>n</c>.</param>
    /// <param name="superDiagonal">
    /// Super-diagonal band of length <c>n − 1</c>
    /// (element <c>[i]</c> occupies row <c>i</c>, column <c>i + 1</c>).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the band lengths are inconsistent.
    /// </exception>
    public TridiagonalMatrix(Vector subDiagonal, Vector mainDiagonal, Vector superDiagonal)
    {
        ArgumentNullException.ThrowIfNull(subDiagonal);
        ArgumentNullException.ThrowIfNull(mainDiagonal);
        ArgumentNullException.ThrowIfNull(superDiagonal);

        int n = mainDiagonal.Length;
        if (n < 1)
            throw new ArgumentException("Main diagonal must have at least one element.", nameof(mainDiagonal));
        if (subDiagonal.Length != n - 1)
            throw new ArgumentException(
                $"Sub-diagonal length must be n − 1 = {n - 1} (got {subDiagonal.Length}).",
                nameof(subDiagonal));
        if (superDiagonal.Length != n - 1)
            throw new ArgumentException(
                $"Super-diagonal length must be n − 1 = {n - 1} (got {superDiagonal.Length}).",
                nameof(superDiagonal));

        Size   = n;
        _lower = new double[n];
        _main  = new double[n];
        _upper = new double[n];

        for (int i = 0; i < n - 1; i++) _lower[i + 1] = subDiagonal[i];
        for (int i = 0; i < n;     i++) _main[i]       = mainDiagonal[i];
        for (int i = 0; i < n - 1; i++) _upper[i]      = superDiagonal[i];
    }

    // private constructor for Clone
    private TridiagonalMatrix(int n, double[] lower, double[] main, double[] upper)
    {
        Size   = n;
        _lower = (double[])lower.Clone();
        _main  = (double[])main.Clone();
        _upper = (double[])upper.Clone();
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the order of the matrix (number of rows = number of columns).</summary>
    public int Size { get; }

    /// <inheritdoc/>
    public int Rows => Size;

    /// <inheritdoc/>
    public int Cols => Size;

    /// <inheritdoc/>
    public bool IsSquare => true;

    /// <summary>
    /// Gets or sets an element by row and column index.
    /// </summary>
    /// <remarks>
    /// Only positions on the three bands (sub-diagonal, main, super-diagonal) can
    /// hold non-zero values.  Attempting to <em>set</em> a non-band position to a
    /// value other than zero raises <see cref="ArgumentException"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either index is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a non-band position is assigned a non-zero value.
    /// </exception>
    public double this[int row, int col]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Size);
            ArgumentOutOfRangeException.ThrowIfNegative(col);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Size);

            if (row == col)           return _main[row];
            if (row == col + 1)       return _lower[row];
            if (row == col - 1)       return _upper[row];
            return 0.0;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Size);
            ArgumentOutOfRangeException.ThrowIfNegative(col);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Size);

            if (row == col)     { _main[row]  = value; return; }
            if (row == col + 1) { _lower[row] = value; return; }
            if (row == col - 1) { _upper[row] = value; return; }

            if (value != 0.0)
                throw new ArgumentException(
                    $"Cannot set off-band element [{row},{col}] to a non-zero value " +
                    "in a TridiagonalMatrix.");
        }
    }

    // ── Thomas algorithm ──────────────────────────────────────────────────────

    /// <summary>
    /// Solves the linear system <c>Ax = rhs</c> using the Thomas algorithm (O(n)).
    /// </summary>
    /// <param name="rhs">Right-hand side vector of length <see cref="Size"/>.</param>
    /// <returns>Solution vector <c>x</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rhs"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="rhs"/> length does not match <see cref="Size"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the matrix is (numerically) singular.
    /// </exception>
    public Vector Solve(Vector rhs)
    {
        ArgumentNullException.ThrowIfNull(rhs);
        if (rhs.Length != Size)
            throw new ArgumentException(
                $"RHS length ({rhs.Length}) must equal matrix size ({Size}).", nameof(rhs));

        int n = Size;

        // Work on copies of bands so the matrix is not modified.
        var bMod = (double[])_main.Clone();
        var cMod = (double[])_upper.Clone();
        var dMod = rhs.ToArray();

        // Forward sweep
        for (int i = 1; i < n; i++)
        {
            if (bMod[i - 1] == 0.0)
                throw new InvalidOperationException(
                    $"Zero pivot encountered at position {i - 1}; matrix may be singular.");

            double w = _lower[i] / bMod[i - 1];
            bMod[i] -= w * cMod[i - 1];
            dMod[i] -= w * dMod[i - 1];
        }

        // Back substitution
        var x = new double[n];
        if (bMod[n - 1] == 0.0)
            throw new InvalidOperationException(
                $"Zero pivot encountered at position {n - 1}; matrix may be singular.");

        x[n - 1] = dMod[n - 1] / bMod[n - 1];
        for (int i = n - 2; i >= 0; i--)
        {
            if (bMod[i] == 0.0)
                throw new InvalidOperationException(
                    $"Zero pivot encountered at position {i}; matrix may be singular.");
            x[i] = (dMod[i] - cMod[i] * x[i + 1]) / bMod[i];
        }

        return new Vector(x);
    }

    // ── Matrix–vector product ─────────────────────────────────────────────────

    /// <summary>
    /// Computes the matrix–vector product <c>Av</c>.
    /// </summary>
    /// <param name="v">Vector of length <see cref="Size"/>.</param>
    /// <returns>Result vector of length <see cref="Size"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="v"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the vector length does not match <see cref="Size"/>.
    /// </exception>
    public Vector Multiply(Vector v)
    {
        ArgumentNullException.ThrowIfNull(v);
        if (v.Length != Size)
            throw new ArgumentException(
                $"Vector length ({v.Length}) must equal matrix size ({Size}).", nameof(v));

        var result = new Vector(Size);
        if (Size == 1)
        {
            result[0] = _main[0] * v[0];
            return result;
        }
        result[0] = _main[0] * v[0] + _upper[0] * v[1];
        for (int i = 1; i < Size - 1; i++)
            result[i] = _lower[i] * v[i - 1] + _main[i] * v[i] + _upper[i] * v[i + 1];
        result[Size - 1] = _lower[Size - 1] * v[Size - 2] + _main[Size - 1] * v[Size - 1];
        return result;
    }

    /// <summary>Matrix–vector multiplication operator.</summary>
    public static Vector operator *(TridiagonalMatrix m, Vector v) => m.Multiply(v);

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>Returns a dense <see cref="Matrix"/> equivalent to this tridiagonal matrix.</summary>
    public Matrix ToDenseMatrix()
    {
        var result = new Matrix(Size, Size);
        result[0, 0] = _main[0];
        if (Size > 1) result[0, 1] = _upper[0];
        for (int i = 1; i < Size - 1; i++)
        {
            result[i, i - 1] = _lower[i];
            result[i, i]     = _main[i];
            result[i, i + 1] = _upper[i];
        }
        if (Size > 1)
        {
            result[Size - 1, Size - 2] = _lower[Size - 1];
            result[Size - 1, Size - 1] = _main[Size - 1];
        }
        return result;
    }

    // ── ICloneable ────────────────────────────────────────────────────────────

    /// <summary>Returns a deep copy of this matrix.</summary>
    public TridiagonalMatrix Clone() => new(Size, _lower, _main, _upper);

    object ICloneable.Clone() => Clone();

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <summary>Determines element-wise equality.</summary>
    public bool Equals(TridiagonalMatrix? other)
    {
        if (other is null || other.Size != Size) return false;
        for (int i = 0; i < Size; i++)
        {
            if (_main[i] != other._main[i]) return false;
            if (_lower[i] != other._lower[i]) return false;
            if (_upper[i] != other._upper[i]) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TridiagonalMatrix t && Equals(t);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(Size);
        foreach (double x in _main)  hc.Add(x);
        foreach (double x in _lower) hc.Add(x);
        foreach (double x in _upper) hc.Add(x);
        return hc.ToHashCode();
    }

    /// <summary>Element-wise equality operator.</summary>
    public static bool operator ==(TridiagonalMatrix? a, TridiagonalMatrix? b) =>
        a is null ? b is null : a.Equals(b);

    /// <summary>Element-wise inequality operator.</summary>
    public static bool operator !=(TridiagonalMatrix? a, TridiagonalMatrix? b) => !(a == b);

    // ── Formatting ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Size; i++)
        {
            sb.Append('[');
            for (int j = 0; j < Size; j++)
            {
                if (j > 0) sb.Append(", ");
                sb.Append(this[i, j]);
            }
            sb.Append(']');
            if (i < Size - 1) sb.AppendLine();
        }
        return sb.ToString();
    }
}

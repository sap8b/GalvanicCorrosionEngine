namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// A real-valued sparse matrix stored in Dictionary of Keys (DOK) format.
/// </summary>
/// <remarks>
/// <para>
/// Only non-zero entries are stored, making this representation memory-efficient
/// for matrices that are predominantly zero — a common pattern in PDE discretisations
/// (e.g., tridiagonal, pentadiagonal, and finite-element stiffness matrices).
/// </para>
/// <para>
/// Consolidated and generalised from the <c>TridiagonalMatrix</c> class in
/// <c>Diffusion2D_Library</c> to support arbitrary sparsity patterns.
/// </para>
/// <para>
/// For highest performance on production-scale systems, consider converting to
/// CSR (Compressed Sparse Row) format via <see cref="ToArray"/> once the sparsity
/// pattern is fixed.
/// </para>
/// </remarks>
public sealed class SparseMatrix : ICloneable
{
    // DOK storage: only non-zero entries are kept.
    private readonly Dictionary<(int row, int col), double> _entries;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialises an empty sparse matrix of the given dimensions.</summary>
    /// <param name="rows">Number of rows.  Must be greater than zero.</param>
    /// <param name="cols">Number of columns.  Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either dimension is ≤ 0.
    /// </exception>
    public SparseMatrix(int rows, int cols)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cols);
        Rows     = rows;
        Cols     = cols;
        _entries = [];
    }

    /// <summary>
    /// Initialises a sparse matrix from a dense two-dimensional array, storing
    /// only the entries whose absolute value exceeds <paramref name="zeroThreshold"/>.
    /// </summary>
    /// <param name="values">Source data.  Must not be <see langword="null"/>.</param>
    /// <param name="zeroThreshold">
    /// Entries whose absolute value is ≤ this threshold are treated as structural
    /// zeros and are not stored.  Defaults to 0 (exact zero test).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public SparseMatrix(double[,] values, double zeroThreshold = 0.0)
    {
        ArgumentNullException.ThrowIfNull(values);
        Rows     = values.GetLength(0);
        Cols     = values.GetLength(1);
        _entries = [];

        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Cols; j++)
                if (Math.Abs(values[i, j]) > zeroThreshold)
                    _entries[(i, j)] = values[i, j];
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a sparse n × n identity matrix.
    /// </summary>
    /// <param name="n">Order of the matrix.  Must be greater than zero.</param>
    public static SparseMatrix Identity(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        var m = new SparseMatrix(n, n);
        for (int i = 0; i < n; i++) m._entries[(i, i)] = 1.0;
        return m;
    }

    /// <summary>
    /// Creates a sparse tridiagonal matrix from three vectors representing the
    /// sub-diagonal, main diagonal, and super-diagonal bands.
    /// </summary>
    /// <param name="subDiagonal">
    /// Vector of length n−1 for entries at position (i, i−1).
    /// </param>
    /// <param name="mainDiagonal">
    /// Vector of length n for entries at position (i, i).
    /// </param>
    /// <param name="superDiagonal">
    /// Vector of length n−1 for entries at position (i, i+1).
    /// </param>
    /// <returns>A sparse n × n tridiagonal matrix.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the band lengths are inconsistent.
    /// </exception>
    public static SparseMatrix Tridiagonal(Vector subDiagonal, Vector mainDiagonal, Vector superDiagonal)
    {
        ArgumentNullException.ThrowIfNull(subDiagonal);
        ArgumentNullException.ThrowIfNull(mainDiagonal);
        ArgumentNullException.ThrowIfNull(superDiagonal);

        int n = mainDiagonal.Length;
        if (subDiagonal.Length   != n - 1)
            throw new ArgumentException($"subDiagonal must have length n−1 = {n - 1}.", nameof(subDiagonal));
        if (superDiagonal.Length != n - 1)
            throw new ArgumentException($"superDiagonal must have length n−1 = {n - 1}.", nameof(superDiagonal));

        var m = new SparseMatrix(n, n);
        for (int i = 0; i < n; i++)
            m._entries[(i, i)] = mainDiagonal[i];
        for (int i = 0; i < n - 1; i++)
        {
            m._entries[(i + 1, i)] = subDiagonal[i];   // sub-diagonal
            m._entries[(i, i + 1)] = superDiagonal[i]; // super-diagonal
        }
        return m;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    public int Cols { get; }

    /// <summary>
    /// Gets a value indicating whether this matrix is square (Rows == Cols).
    /// </summary>
    public bool IsSquare => Rows == Cols;

    /// <summary>Gets the number of explicitly stored (non-zero) entries.</summary>
    public int NonZeroCount => _entries.Count;

    /// <summary>
    /// Gets or sets the element at row <paramref name="row"/>, column <paramref name="col"/>.
    /// Setting a value to zero removes the entry from storage.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either index is out of range.
    /// </exception>
    public double this[int row, int col]
    {
        get
        {
            ValidateIndices(row, col);
            return _entries.TryGetValue((row, col), out double v) ? v : 0.0;
        }
        set
        {
            ValidateIndices(row, col);
            if (value == 0.0)
                _entries.Remove((row, col));
            else
                _entries[(row, col)] = value;
        }
    }

    // ── Matrix–vector multiply ────────────────────────────────────────────────

    /// <summary>
    /// Multiplies this matrix by a column vector y = A·x.
    /// </summary>
    /// <param name="x">Input vector of length <see cref="Cols"/>.</param>
    /// <returns>Result vector of length <see cref="Rows"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="x"/> has the wrong length.
    /// </exception>
    public Vector Multiply(Vector x)
    {
        ArgumentNullException.ThrowIfNull(x);
        if (x.Length != Cols)
            throw new ArgumentException(
                $"Vector length ({x.Length}) must equal the number of matrix columns ({Cols}).",
                nameof(x));

        var result = new Vector(Rows);
        foreach (var ((row, col), value) in _entries)
            result[row] += value * x[col];
        return result;
    }

    /// <inheritdoc cref="Multiply(Vector)"/>
    public static Vector operator *(SparseMatrix m, Vector v) => m.Multiply(v);

    // ── Dense conversion ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full dense representation of this matrix as a two-dimensional
    /// array.  Structural zeros are filled with 0.0.
    /// </summary>
    public double[,] ToArray()
    {
        var result = new double[Rows, Cols];
        foreach (var ((row, col), value) in _entries)
            result[row, col] = value;
        return result;
    }

    /// <summary>
    /// Returns a dense <see cref="Matrix"/> representation of this sparse matrix.
    /// Structural zeros are filled with 0.0.
    /// </summary>
    public Matrix ToDenseMatrix() => new(ToArray());

    // ── ICloneable ────────────────────────────────────────────────────────────

    /// <summary>Returns a deep copy of this sparse matrix.</summary>
    public SparseMatrix Clone()
    {
        var copy = new SparseMatrix(Rows, Cols);
        foreach (var (key, value) in _entries)
            copy._entries[key] = value;
        return copy;
    }

    object ICloneable.Clone() => Clone();

    // ── Formatting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a string listing the non-zero entries in (row, col) = value format.
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"SparseMatrix {Rows}×{Cols} ({NonZeroCount} non-zeros):");
        foreach (var ((row, col), value) in _entries)
            sb.AppendLine($"  [{row},{col}] = {value}");
        return sb.ToString().TrimEnd();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ValidateIndices(int row, int col)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfNegative(col);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Cols);
    }
}

namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// A dense, real-valued m × n matrix with standard linear-algebra operations.
/// </summary>
/// <remarks>
/// Consolidated from <c>Diffusion2D_Library.RMatrix</c> with modernised C# conventions.
/// Storage is row-major.  All arithmetic operators return new instances.
/// </remarks>
public sealed class Matrix : IMatrix, IEquatable<Matrix>
{
    private readonly double[,] _data;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialises a zero matrix of the given dimensions.</summary>
    /// <param name="rows">Number of rows.  Must be greater than zero.</param>
    /// <param name="cols">Number of columns.  Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either dimension is ≤ 0.
    /// </exception>
    public Matrix(int rows, int cols)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cols);
        Rows  = rows;
        Cols  = cols;
        _data = new double[rows, cols];
    }

    /// <summary>
    /// Initialises a matrix from a two-dimensional array.
    /// The array is copied so that subsequent changes to the caller's array do
    /// not affect this instance.
    /// </summary>
    /// <param name="values">Source data.  Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public Matrix(double[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Rows  = values.GetLength(0);
        Cols  = values.GetLength(1);
        _data = (double[,])values.Clone();
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>Creates an n × n identity matrix.</summary>
    /// <param name="n">Order of the matrix.  Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="n"/> is ≤ 0.
    /// </exception>
    public static Matrix Identity(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        var m = new Matrix(n, n);
        for (int i = 0; i < n; i++) m._data[i, i] = 1.0;
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

    /// <summary>Gets or sets the element at row <paramref name="row"/>, column <paramref name="col"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either index is out of range.
    /// </exception>
    public double this[int row, int col]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            ArgumentOutOfRangeException.ThrowIfNegative(col);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Cols);
            return _data[row, col];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            ArgumentOutOfRangeException.ThrowIfNegative(col);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Cols);
            _data[row, col] = value;
        }
    }

    // ── Row and column accessors ──────────────────────────────────────────────

    /// <summary>Returns a copy of the specified row as a <see cref="Vector"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row"/> is out of range.
    /// </exception>
    public Vector GetRow(int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        var v = new Vector(Cols);
        for (int j = 0; j < Cols; j++) v[j] = _data[row, j];
        return v;
    }

    /// <summary>Returns a copy of the specified column as a <see cref="Vector"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="col"/> is out of range.
    /// </exception>
    public Vector GetColumn(int col)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(col);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Cols);
        var v = new Vector(Rows);
        for (int i = 0; i < Rows; i++) v[i] = _data[i, col];
        return v;
    }

    // ── Matrix properties ─────────────────────────────────────────────────────

    /// <summary>
    /// Computes the trace (sum of diagonal elements).  Only defined for square matrices.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the matrix is not square.
    /// </exception>
    public double Trace()
    {
        if (!IsSquare)
            throw new InvalidOperationException("Trace is only defined for square matrices.");
        double sum = 0.0;
        for (int i = 0; i < Rows; i++) sum += _data[i, i];
        return sum;
    }

    /// <summary>Returns the transpose of this matrix as a new <see cref="Matrix"/>.</summary>
    public Matrix Transpose()
    {
        var result = new Matrix(Cols, Rows);
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Cols; j++)
                result._data[j, i] = _data[i, j];
        return result;
    }

    // ── Arithmetic operators ──────────────────────────────────────────────────

    /// <summary>Returns a copy of the matrix (unary +).</summary>
    public static Matrix operator +(Matrix m) => m.Clone();

    /// <summary>Returns the element-wise negation of the matrix (unary -).</summary>
    public static Matrix operator -(Matrix m)
    {
        var result = new Matrix(m.Rows, m.Cols);
        for (int i = 0; i < m.Rows; i++)
            for (int j = 0; j < m.Cols; j++)
                result._data[i, j] = -m._data[i, j];
        return result;
    }

    /// <summary>Element-wise addition of two matrices.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the matrices have different dimensions.
    /// </exception>
    public static Matrix operator +(Matrix m1, Matrix m2)
    {
        ThrowIfDimensionMismatch(m1, m2);
        var result = new Matrix(m1.Rows, m1.Cols);
        for (int i = 0; i < m1.Rows; i++)
            for (int j = 0; j < m1.Cols; j++)
                result._data[i, j] = m1._data[i, j] + m2._data[i, j];
        return result;
    }

    /// <summary>Adds a scalar to every element.</summary>
    public static Matrix operator +(Matrix m, double scalar)
    {
        var result = new Matrix(m.Rows, m.Cols);
        for (int i = 0; i < m.Rows; i++)
            for (int j = 0; j < m.Cols; j++)
                result._data[i, j] = m._data[i, j] + scalar;
        return result;
    }

    /// <inheritdoc cref="operator +(Matrix, double)"/>
    public static Matrix operator +(double scalar, Matrix m) => m + scalar;

    /// <summary>Element-wise subtraction of two matrices.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the matrices have different dimensions.
    /// </exception>
    public static Matrix operator -(Matrix m1, Matrix m2)
    {
        ThrowIfDimensionMismatch(m1, m2);
        var result = new Matrix(m1.Rows, m1.Cols);
        for (int i = 0; i < m1.Rows; i++)
            for (int j = 0; j < m1.Cols; j++)
                result._data[i, j] = m1._data[i, j] - m2._data[i, j];
        return result;
    }

    /// <summary>Subtracts a scalar from every element.</summary>
    public static Matrix operator -(Matrix m, double scalar)
    {
        var result = new Matrix(m.Rows, m.Cols);
        for (int i = 0; i < m.Rows; i++)
            for (int j = 0; j < m.Cols; j++)
                result._data[i, j] = m._data[i, j] - scalar;
        return result;
    }

    /// <summary>Multiplies every element by a scalar.</summary>
    public static Matrix operator *(Matrix m, double scalar)
    {
        var result = new Matrix(m.Rows, m.Cols);
        for (int i = 0; i < m.Rows; i++)
            for (int j = 0; j < m.Cols; j++)
                result._data[i, j] = m._data[i, j] * scalar;
        return result;
    }

    /// <inheritdoc cref="operator *(Matrix, double)"/>
    public static Matrix operator *(double scalar, Matrix m) => m * scalar;

    /// <summary>
    /// Matrix multiplication (m1 × m2).  The inner dimensions must agree:
    /// <c>m1.Cols == m2.Rows</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the inner dimensions do not agree.
    /// </exception>
    public static Matrix operator *(Matrix m1, Matrix m2)
    {
        if (m1.Cols != m2.Rows)
            throw new ArgumentException(
                $"Inner dimensions must agree for matrix multiplication " +
                $"(got {m1.Rows}×{m1.Cols} and {m2.Rows}×{m2.Cols}).",
                nameof(m2));

        var result = new Matrix(m1.Rows, m2.Cols);
        for (int i = 0; i < m1.Rows; i++)
            for (int j = 0; j < m2.Cols; j++)
            {
                double sum = 0.0;
                for (int k = 0; k < m1.Cols; k++)
                    sum += m1._data[i, k] * m2._data[k, j];
                result._data[i, j] = sum;
            }
        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a column vector (m × v).
    /// The vector length must equal the number of columns in the matrix.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when dimensions do not agree.
    /// </exception>
    public static Vector operator *(Matrix m, Vector v)
    {
        if (m.Cols != v.Length)
            throw new ArgumentException(
                $"Matrix column count ({m.Cols}) must equal vector length ({v.Length}).",
                nameof(v));

        var result = new Vector(m.Rows);
        for (int i = 0; i < m.Rows; i++)
        {
            double sum = 0.0;
            for (int j = 0; j < m.Cols; j++)
                sum += m._data[i, j] * v[j];
            result[i] = sum;
        }
        return result;
    }

    /// <summary>Divides every element by a scalar.</summary>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="scalar"/> is zero.
    /// </exception>
    public static Matrix operator /(Matrix m, double scalar)
    {
        if (scalar == 0.0) throw new DivideByZeroException("Scalar divisor must not be zero.");
        var result = new Matrix(m.Rows, m.Cols);
        for (int i = 0; i < m.Rows; i++)
            for (int j = 0; j < m.Cols; j++)
                result._data[i, j] = m._data[i, j] / scalar;
        return result;
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines element-wise equality using exact floating-point comparison.
    /// </summary>
    public bool Equals(Matrix? other)
    {
        if (other is null || other.Rows != Rows || other.Cols != Cols) return false;
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Cols; j++)
                if (_data[i, j] != other._data[i, j]) return false;
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Matrix m && Equals(m);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(Rows);
        hc.Add(Cols);
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Cols; j++)
                hc.Add(_data[i, j]);
        return hc.ToHashCode();
    }

    /// <summary>Element-wise equality operator.</summary>
    public static bool operator ==(Matrix? m1, Matrix? m2) =>
        m1 is null ? m2 is null : m1.Equals(m2);

    /// <summary>Element-wise inequality operator.</summary>
    public static bool operator !=(Matrix? m1, Matrix? m2) => !(m1 == m2);

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a deep copy of the underlying data as a new two-dimensional array.
    /// </summary>
    public double[,] ToArray() => (double[,])_data.Clone();

    // ── ICloneable ────────────────────────────────────────────────────────────

    /// <summary>Returns a deep copy of this matrix.</summary>
    public Matrix Clone()
    {
        var copy = new Matrix(Rows, Cols);
        Array.Copy(_data, copy._data, _data.Length);
        return copy;
    }

    object ICloneable.Clone() => Clone();

    // ── Formatting ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Rows; i++)
        {
            sb.Append('[');
            for (int j = 0; j < Cols; j++)
            {
                if (j > 0) sb.Append(", ");
                sb.Append(_data[i, j]);
            }
            sb.Append(']');
            if (i < Rows - 1) sb.AppendLine();
        }
        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ThrowIfDimensionMismatch(Matrix m1, Matrix m2)
    {
        if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
            throw new ArgumentException(
                $"Matrices must have the same dimensions " +
                $"(got {m1.Rows}×{m1.Cols} and {m2.Rows}×{m2.Cols}).");
    }
}

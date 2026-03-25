namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// A fixed-length, real-valued vector with standard linear-algebra operations.
/// </summary>
/// <remarks>
/// Consolidated from <c>Diffusion2D_Library.RVector</c> with modernised C# conventions.
/// All arithmetic operators create new instances; the original data is never mutated
/// unless the caller explicitly assigns through the indexer.
/// </remarks>
public sealed class Vector : IVector, IEquatable<Vector>
{
    private readonly double[] _data;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialises a zero vector of the given length.</summary>
    /// <param name="length">Number of elements.  Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="length"/> is less than or equal to zero.
    /// </exception>
    public Vector(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        _data  = new double[length];
        Length = length;
    }

    /// <summary>
    /// Initialises a vector from an existing array.  The array is copied so that
    /// subsequent changes to the caller's array do not affect this instance.
    /// </summary>
    /// <param name="values">Source values.  Must not be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="values"/> is empty.
    /// </exception>
    public Vector(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("Array must not be empty.", nameof(values));

        _data  = (double[])values.Clone();
        Length = _data.Length;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the number of elements in this vector.</summary>
    public int Length { get; }

    /// <summary>Gets or sets the element at the given zero-based index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside [0, <see cref="Length"/>).
    /// </exception>
    public double this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return _data[index];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            _data[index] = value;
        }
    }

    // ── Slicing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new vector that is a contiguous sub-section of this vector.
    /// </summary>
    /// <param name="startIndex">Inclusive start index (zero-based).</param>
    /// <param name="endIndex">Exclusive end index (zero-based).</param>
    /// <returns>A new <see cref="Vector"/> of length <c>endIndex − startIndex</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either index is out of range or <paramref name="startIndex"/>
    /// ≥ <paramref name="endIndex"/>.
    /// </exception>
    public Vector Slice(int startIndex, int endIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endIndex, Length);
        if (startIndex >= endIndex)
            throw new ArgumentOutOfRangeException(nameof(startIndex),
                "startIndex must be less than endIndex.");

        int count  = endIndex - startIndex;
        var result = new Vector(count);
        Array.Copy(_data, startIndex, result._data, 0, count);
        return result;
    }

    // ── Norms and normalisation ───────────────────────────────────────────────

    /// <summary>Computes the Euclidean (L2) norm ‖v‖₂.</summary>
    public double Norm()
    {
        double sum = 0.0;
        foreach (double x in _data) sum += x * x;
        return Math.Sqrt(sum);
    }

    /// <summary>Computes the square of the Euclidean norm ‖v‖₂².</summary>
    public double NormSquared()
    {
        double sum = 0.0;
        foreach (double x in _data) sum += x * x;
        return sum;
    }

    /// <summary>
    /// Returns a new unit vector in the same direction as this vector.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this vector has zero norm.
    /// </exception>
    public Vector GetUnitVector()
    {
        double norm = Norm();
        if (norm == 0.0)
            throw new InvalidOperationException("Cannot normalise a zero vector.");
        return this / norm;
    }

    // ── Static mathematical operations ───────────────────────────────────────

    /// <summary>
    /// Computes the dot product v₁ · v₂.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the vectors have different lengths.
    /// </exception>
    public static double Dot(Vector v1, Vector v2)
    {
        ThrowIfLengthMismatch(v1, v2, nameof(v2));
        double result = 0.0;
        for (int i = 0; i < v1.Length; i++)
            result += v1._data[i] * v2._data[i];
        return result;
    }

    /// <summary>
    /// Computes the element-wise product of two vectors.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the vectors have different lengths.
    /// </exception>
    public static Vector ElementwiseProduct(Vector v1, Vector v2)
    {
        ThrowIfLengthMismatch(v1, v2, nameof(v2));
        var result = new Vector(v1.Length);
        for (int i = 0; i < v1.Length; i++)
            result._data[i] = v1._data[i] * v2._data[i];
        return result;
    }

    /// <summary>
    /// Computes the 3-D cross product v₁ × v₂.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when either vector is not of length 3.
    /// </exception>
    public static Vector CrossProduct(Vector v1, Vector v2)
    {
        if (v1.Length != 3 || v2.Length != 3)
            throw new ArgumentException("Cross product is only defined for 3-dimensional vectors.");

        return new Vector([
            v1[1] * v2[2] - v1[2] * v2[1],
            v1[2] * v2[0] - v1[0] * v2[2],
            v1[0] * v2[1] - v1[1] * v2[0],
        ]);
    }

    // ── Arithmetic operators ──────────────────────────────────────────────────

    /// <summary>Returns a copy of the vector (unary +).</summary>
    public static Vector operator +(Vector v) => v.Clone();

    /// <summary>Returns the negation of the vector (unary -).</summary>
    public static Vector operator -(Vector v)
    {
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = -v._data[i];
        return result;
    }

    /// <summary>Element-wise addition of two vectors.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the vectors have different lengths.
    /// </exception>
    public static Vector operator +(Vector v1, Vector v2)
    {
        ThrowIfLengthMismatch(v1, v2, nameof(v2));
        var result = new Vector(v1.Length);
        for (int i = 0; i < v1.Length; i++) result._data[i] = v1._data[i] + v2._data[i];
        return result;
    }

    /// <summary>Adds a scalar to every element.</summary>
    public static Vector operator +(Vector v, double scalar)
    {
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = v._data[i] + scalar;
        return result;
    }

    /// <inheritdoc cref="operator +(Vector, double)"/>
    public static Vector operator +(double scalar, Vector v) => v + scalar;

    /// <summary>Element-wise subtraction of two vectors.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the vectors have different lengths.
    /// </exception>
    public static Vector operator -(Vector v1, Vector v2)
    {
        ThrowIfLengthMismatch(v1, v2, nameof(v2));
        var result = new Vector(v1.Length);
        for (int i = 0; i < v1.Length; i++) result._data[i] = v1._data[i] - v2._data[i];
        return result;
    }

    /// <summary>Subtracts a scalar from every element.</summary>
    public static Vector operator -(Vector v, double scalar)
    {
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = v._data[i] - scalar;
        return result;
    }

    /// <summary>Subtracts every element from a scalar (scalar - v).</summary>
    public static Vector operator -(double scalar, Vector v)
    {
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = scalar - v._data[i];
        return result;
    }

    /// <summary>Multiplies every element by a scalar.</summary>
    public static Vector operator *(Vector v, double scalar)
    {
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = v._data[i] * scalar;
        return result;
    }

    /// <inheritdoc cref="operator *(Vector, double)"/>
    public static Vector operator *(double scalar, Vector v) => v * scalar;

    /// <summary>Divides every element by a scalar.</summary>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="scalar"/> is zero.
    /// </exception>
    public static Vector operator /(Vector v, double scalar)
    {
        if (scalar == 0.0) throw new DivideByZeroException("Scalar divisor must not be zero.");
        var result = new Vector(v.Length);
        for (int i = 0; i < v.Length; i++) result._data[i] = v._data[i] / scalar;
        return result;
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether two vectors are element-wise equal using
    /// exact floating-point comparison.
    /// </summary>
    public bool Equals(Vector? other)
    {
        if (other is null || other.Length != Length) return false;
        for (int i = 0; i < Length; i++)
            if (_data[i] != other._data[i]) return false;
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Vector v && Equals(v);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (double x in _data) hc.Add(x);
        return hc.ToHashCode();
    }

    /// <summary>Element-wise equality operator.</summary>
    public static bool operator ==(Vector? v1, Vector? v2) =>
        v1 is null ? v2 is null : v1.Equals(v2);

    /// <summary>Element-wise inequality operator.</summary>
    public static bool operator !=(Vector? v1, Vector? v2) => !(v1 == v2);

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>Returns a copy of the underlying data as a new array.</summary>
    public double[] ToArray() => (double[])_data.Clone();

    // ── ICloneable ────────────────────────────────────────────────────────────

    /// <summary>Returns a deep copy of this vector.</summary>
    public Vector Clone()
    {
        var copy = new Vector(Length);
        Array.Copy(_data, copy._data, Length);
        return copy;
    }

    object ICloneable.Clone() => Clone();

    // ── Formatting ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString()
    {
        return "[" + string.Join(", ", _data) + "]";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ThrowIfLengthMismatch(Vector v1, Vector v2, string paramName)
    {
        if (v1.Length != v2.Length)
            throw new ArgumentException(
                $"Vectors must have the same length (got {v1.Length} and {v2.Length}).",
                paramName);
    }
}

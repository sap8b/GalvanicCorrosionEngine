namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// Defines the minimal contract for a real-valued matrix.
/// </summary>
public interface IMatrix : ICloneable
{
    /// <summary>Gets the number of rows.</summary>
    int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    int Cols { get; }

    /// <summary>Gets a value indicating whether the matrix is square (Rows == Cols).</summary>
    bool IsSquare { get; }

    /// <summary>Gets or sets the element at the given row and column.</summary>
    double this[int row, int col] { get; set; }
}

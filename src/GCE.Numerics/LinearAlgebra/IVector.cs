namespace GCE.Numerics.LinearAlgebra;

/// <summary>
/// Defines the minimal contract for a real-valued, fixed-length vector.
/// </summary>
public interface IVector : ICloneable
{
    /// <summary>Gets the number of elements.</summary>
    int Length { get; }

    /// <summary>Gets or sets the element at the given zero-based index.</summary>
    double this[int index] { get; set; }
}

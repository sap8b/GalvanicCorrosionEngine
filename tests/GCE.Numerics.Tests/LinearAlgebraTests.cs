using GCE.Numerics.LinearAlgebra;

namespace GCE.Numerics.Tests;

// ═════════════════════════════════════════════════════════════════════════════
// Vector tests
// ═════════════════════════════════════════════════════════════════════════════

public class VectorConstructorTests
{
    [Fact]
    public void Vector_LengthConstructor_CreatesZeroVector()
    {
        var v = new Vector(4);
        Assert.Equal(4, v.Length);
        for (int i = 0; i < 4; i++) Assert.Equal(0.0, v[i]);
    }

    [Fact]
    public void Vector_LengthConstructor_ThrowsForZeroLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector(0));
    }

    [Fact]
    public void Vector_LengthConstructor_ThrowsForNegativeLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector(-1));
    }

    [Fact]
    public void Vector_ArrayConstructor_CopiesValues()
    {
        double[] arr = [1.0, 2.0, 3.0];
        var v = new Vector(arr);
        Assert.Equal(3, v.Length);
        Assert.Equal(1.0, v[0]);
        Assert.Equal(2.0, v[1]);
        Assert.Equal(3.0, v[2]);
    }

    [Fact]
    public void Vector_ArrayConstructor_IsIndependentOfSourceArray()
    {
        double[] arr = [1.0, 2.0, 3.0];
        var v = new Vector(arr);
        arr[0] = 99.0;
        Assert.Equal(1.0, v[0]); // unchanged
    }

    [Fact]
    public void Vector_ArrayConstructor_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Vector((double[])null!));
    }

    [Fact]
    public void Vector_ArrayConstructor_ThrowsForEmptyArray()
    {
        Assert.Throws<ArgumentException>(() => new Vector(Array.Empty<double>()));
    }
}

public class VectorIndexerTests
{
    [Fact]
    public void Vector_Indexer_GetAndSet_WorkCorrectly()
    {
        var v = new Vector(3);
        v[0] = 5.0;
        v[2] = -3.14;
        Assert.Equal(5.0,   v[0]);
        Assert.Equal(0.0,   v[1]);
        Assert.Equal(-3.14, v[2]);
    }

    [Fact]
    public void Vector_Indexer_Get_ThrowsWhenOutOfRange()
    {
        var v = new Vector(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = v[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = v[-1]);
    }

    [Fact]
    public void Vector_Indexer_Set_ThrowsWhenOutOfRange()
    {
        var v = new Vector(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[3] = 1.0);
    }
}

public class VectorSliceTests
{
    [Fact]
    public void Vector_Slice_ReturnsCorrectSubVector()
    {
        var v = new Vector([1.0, 2.0, 3.0, 4.0, 5.0]);
        var s = v.Slice(1, 4);
        Assert.Equal(3, s.Length);
        Assert.Equal(2.0, s[0]);
        Assert.Equal(3.0, s[1]);
        Assert.Equal(4.0, s[2]);
    }

    [Fact]
    public void Vector_Slice_ThrowsWhenStartEqualsEnd()
    {
        var v = new Vector([1.0, 2.0, 3.0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v.Slice(1, 1));
    }
}

public class VectorNormTests
{
    [Fact]
    public void Vector_Norm_IsCorrect()
    {
        var v = new Vector([3.0, 4.0]);
        Assert.Equal(5.0, v.Norm(), precision: 12);
    }

    [Fact]
    public void Vector_NormSquared_IsCorrect()
    {
        var v = new Vector([3.0, 4.0]);
        Assert.Equal(25.0, v.NormSquared(), precision: 12);
    }

    [Fact]
    public void Vector_GetUnitVector_HasNormOne()
    {
        var v = new Vector([3.0, 4.0]);
        var u = v.GetUnitVector();
        Assert.Equal(1.0, u.Norm(), precision: 12);
        Assert.Equal(0.6, u[0], precision: 12);
        Assert.Equal(0.8, u[1], precision: 12);
    }

    [Fact]
    public void Vector_GetUnitVector_ThrowsForZeroVector()
    {
        var v = new Vector(3);
        Assert.Throws<InvalidOperationException>(() => v.GetUnitVector());
    }
}

public class VectorStaticOperationTests
{
    [Fact]
    public void Vector_Dot_IsCorrect()
    {
        var v1 = new Vector([1.0, 2.0, 3.0]);
        var v2 = new Vector([4.0, 5.0, 6.0]);
        Assert.Equal(32.0, Vector.Dot(v1, v2), precision: 12);
    }

    [Fact]
    public void Vector_Dot_ThrowsForLengthMismatch()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([1.0, 2.0, 3.0]);
        Assert.Throws<ArgumentException>(() => Vector.Dot(v1, v2));
    }

    [Fact]
    public void Vector_ElementwiseProduct_IsCorrect()
    {
        var v1 = new Vector([2.0, 3.0, 4.0]);
        var v2 = new Vector([5.0, 6.0, 7.0]);
        var r  = Vector.ElementwiseProduct(v1, v2);
        Assert.Equal(10.0, r[0]);
        Assert.Equal(18.0, r[1]);
        Assert.Equal(28.0, r[2]);
    }

    [Fact]
    public void Vector_CrossProduct_IsCorrect()
    {
        var i = new Vector([1.0, 0.0, 0.0]);
        var j = new Vector([0.0, 1.0, 0.0]);
        var k = Vector.CrossProduct(i, j);
        Assert.Equal(0.0, k[0], precision: 12);
        Assert.Equal(0.0, k[1], precision: 12);
        Assert.Equal(1.0, k[2], precision: 12);
    }

    [Fact]
    public void Vector_CrossProduct_ThrowsForNon3D()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([3.0, 4.0]);
        Assert.Throws<ArgumentException>(() => Vector.CrossProduct(v1, v2));
    }
}

public class VectorArithmeticTests
{
    [Fact]
    public void Vector_UnaryPlus_ReturnsCopy()
    {
        var v = new Vector([1.0, 2.0]);
        var r = +v;
        Assert.Equal(v, r);
        Assert.NotSame(v, r);
    }

    [Fact]
    public void Vector_UnaryMinus_NegatesElements()
    {
        var v = new Vector([1.0, -2.0, 3.0]);
        var r = -v;
        Assert.Equal(-1.0, r[0]);
        Assert.Equal(2.0,  r[1]);
        Assert.Equal(-3.0, r[2]);
    }

    [Fact]
    public void Vector_Addition_ElementWise()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([3.0, 4.0]);
        var r  = v1 + v2;
        Assert.Equal(4.0, r[0]);
        Assert.Equal(6.0, r[1]);
    }

    [Fact]
    public void Vector_Addition_ThrowsForLengthMismatch()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([1.0]);
        Assert.Throws<ArgumentException>(() => _ = v1 + v2);
    }

    [Fact]
    public void Vector_ScalarAddition_AddsToEachElement()
    {
        var v = new Vector([1.0, 2.0, 3.0]);
        var r = v + 10.0;
        Assert.Equal(11.0, r[0]);
        Assert.Equal(12.0, r[1]);
        Assert.Equal(13.0, r[2]);
    }

    [Fact]
    public void Vector_Subtraction_ElementWise()
    {
        var v1 = new Vector([5.0, 3.0]);
        var v2 = new Vector([2.0, 1.0]);
        var r  = v1 - v2;
        Assert.Equal(3.0, r[0]);
        Assert.Equal(2.0, r[1]);
    }

    [Fact]
    public void Vector_ScalarMultiplication_ScalesElements()
    {
        var v = new Vector([1.0, 2.0, 3.0]);
        var r = v * 2.0;
        Assert.Equal(2.0, r[0]);
        Assert.Equal(4.0, r[1]);
        Assert.Equal(6.0, r[2]);
    }

    [Fact]
    public void Vector_ScalarMultiplication_IsCommutative()
    {
        var v  = new Vector([1.0, 2.0, 3.0]);
        Assert.Equal(v * 3.0, 3.0 * v);
    }

    [Fact]
    public void Vector_ScalarDivision_DividesElements()
    {
        var v = new Vector([4.0, 8.0]);
        var r = v / 2.0;
        Assert.Equal(2.0, r[0]);
        Assert.Equal(4.0, r[1]);
    }

    [Fact]
    public void Vector_ScalarDivision_ThrowsForZeroDivisor()
    {
        var v = new Vector([1.0, 2.0]);
        Assert.Throws<DivideByZeroException>(() => _ = v / 0.0);
    }
}

public class VectorEqualityTests
{
    [Fact]
    public void Vector_Equals_TrueForIdenticalValues()
    {
        var v1 = new Vector([1.0, 2.0, 3.0]);
        var v2 = new Vector([1.0, 2.0, 3.0]);
        Assert.True(v1.Equals(v2));
        Assert.True(v1 == v2);
    }

    [Fact]
    public void Vector_Equals_FalseForDifferentValues()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([1.0, 3.0]);
        Assert.False(v1.Equals(v2));
        Assert.True(v1 != v2);
    }

    [Fact]
    public void Vector_Equals_FalseForDifferentLength()
    {
        var v1 = new Vector([1.0, 2.0]);
        var v2 = new Vector([1.0, 2.0, 3.0]);
        Assert.False(v1.Equals(v2));
    }
}

public class VectorCloneTests
{
    [Fact]
    public void Vector_Clone_IsDeepCopy()
    {
        var v    = new Vector([1.0, 2.0, 3.0]);
        var copy = v.Clone();
        copy[0]  = 99.0;
        Assert.Equal(1.0, v[0]); // original unchanged
    }

    [Fact]
    public void Vector_ToArray_ReturnsIndependentCopy()
    {
        var v    = new Vector([1.0, 2.0]);
        var arr  = v.ToArray();
        arr[0]   = 99.0;
        Assert.Equal(1.0, v[0]);
    }
}

public class VectorToStringTests
{
    [Fact]
    public void Vector_ToString_ContainsElements()
    {
        var v = new Vector([1.0, 2.5, -3.0]);
        string s = v.ToString();
        Assert.Contains("1", s);
        Assert.Contains("2.5", s);
        Assert.Contains("-3", s);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// Matrix tests
// ═════════════════════════════════════════════════════════════════════════════

public class MatrixConstructorTests
{
    [Fact]
    public void Matrix_DimensionConstructor_CreatesZeroMatrix()
    {
        var m = new Matrix(2, 3);
        Assert.Equal(2, m.Rows);
        Assert.Equal(3, m.Cols);
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 3; j++)
                Assert.Equal(0.0, m[i, j]);
    }

    [Fact]
    public void Matrix_DimensionConstructor_ThrowsForZeroDimension()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Matrix(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Matrix(2, 0));
    }

    [Fact]
    public void Matrix_ArrayConstructor_CopiesValues()
    {
        double[,] arr = { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var m = new Matrix(arr);
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(4.0, m[1, 1]);
    }

    [Fact]
    public void Matrix_ArrayConstructor_IsIndependentOfSourceArray()
    {
        double[,] arr = { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var m = new Matrix(arr);
        arr[0, 0] = 99.0;
        Assert.Equal(1.0, m[0, 0]);
    }

    [Fact]
    public void Matrix_ArrayConstructor_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Matrix((double[,])null!));
    }
}

public class MatrixIdentityTests
{
    [Fact]
    public void Matrix_Identity_HasOnesOnDiagonal()
    {
        var I = Matrix.Identity(3);
        Assert.Equal(3, I.Rows);
        Assert.Equal(3, I.Cols);
        Assert.Equal(1.0, I[0, 0]);
        Assert.Equal(1.0, I[1, 1]);
        Assert.Equal(1.0, I[2, 2]);
        Assert.Equal(0.0, I[0, 1]);
        Assert.Equal(0.0, I[1, 0]);
    }

    [Fact]
    public void Matrix_Identity_ThrowsForZeroOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Identity(0));
    }
}

public class MatrixPropertiesTests
{
    [Fact]
    public void Matrix_IsSquare_TrueForSquareMatrix()
    {
        var m = new Matrix(3, 3);
        Assert.True(m.IsSquare);
    }

    [Fact]
    public void Matrix_IsSquare_FalseForRectangularMatrix()
    {
        var m = new Matrix(2, 3);
        Assert.False(m.IsSquare);
    }

    [Fact]
    public void Matrix_Trace_IsCorrect()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        Assert.Equal(5.0, m.Trace(), precision: 12);
    }

    [Fact]
    public void Matrix_Trace_ThrowsForNonSquare()
    {
        var m = new Matrix(2, 3);
        Assert.Throws<InvalidOperationException>(() => m.Trace());
    }
}

public class MatrixTransposeTests
{
    [Fact]
    public void Matrix_Transpose_CorrectlySwapsDimensions()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } });
        var t = m.Transpose();
        Assert.Equal(3, t.Rows);
        Assert.Equal(2, t.Cols);
        Assert.Equal(1.0, t[0, 0]);
        Assert.Equal(4.0, t[0, 1]);
        Assert.Equal(2.0, t[1, 0]);
        Assert.Equal(5.0, t[1, 1]);
        Assert.Equal(3.0, t[2, 0]);
        Assert.Equal(6.0, t[2, 1]);
    }

    [Fact]
    public void Matrix_Transpose_SquareMatrix_IsCorrect()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var t = m.Transpose();
        Assert.Equal(1.0, t[0, 0]);
        Assert.Equal(3.0, t[0, 1]);
        Assert.Equal(2.0, t[1, 0]);
        Assert.Equal(4.0, t[1, 1]);
    }
}

public class MatrixRowColumnTests
{
    [Fact]
    public void Matrix_GetRow_ReturnsCorrectVector()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } });
        var r = m.GetRow(1);
        Assert.Equal(3, r.Length);
        Assert.Equal(4.0, r[0]);
        Assert.Equal(5.0, r[1]);
        Assert.Equal(6.0, r[2]);
    }

    [Fact]
    public void Matrix_GetColumn_ReturnsCorrectVector()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 }, { 5.0, 6.0 } });
        var c = m.GetColumn(0);
        Assert.Equal(3, c.Length);
        Assert.Equal(1.0, c[0]);
        Assert.Equal(3.0, c[1]);
        Assert.Equal(5.0, c[2]);
    }

    [Fact]
    public void Matrix_GetRow_ThrowsForOutOfRange()
    {
        var m = new Matrix(2, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => m.GetRow(2));
    }
}

public class MatrixArithmeticTests
{
    [Fact]
    public void Matrix_UnaryPlus_ReturnsCopy()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var r = +m;
        Assert.Equal(m, r);
        Assert.NotSame(m, r);
    }

    [Fact]
    public void Matrix_UnaryMinus_NegatesElements()
    {
        var m = new Matrix(new double[,] { { 1.0, -2.0 }, { 3.0, -4.0 } });
        var r = -m;
        Assert.Equal(-1.0, r[0, 0]);
        Assert.Equal(2.0,  r[0, 1]);
        Assert.Equal(-3.0, r[1, 0]);
        Assert.Equal(4.0,  r[1, 1]);
    }

    [Fact]
    public void Matrix_Addition_IsCorrect()
    {
        var m1 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var m2 = new Matrix(new double[,] { { 5.0, 6.0 }, { 7.0, 8.0 } });
        var r  = m1 + m2;
        Assert.Equal(6.0,  r[0, 0]);
        Assert.Equal(8.0,  r[0, 1]);
        Assert.Equal(10.0, r[1, 0]);
        Assert.Equal(12.0, r[1, 1]);
    }

    [Fact]
    public void Matrix_Addition_ThrowsForDimensionMismatch()
    {
        var m1 = new Matrix(2, 2);
        var m2 = new Matrix(2, 3);
        Assert.Throws<ArgumentException>(() => _ = m1 + m2);
    }

    [Fact]
    public void Matrix_ScalarAddition_AddsToEachElement()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var r = m + 10.0;
        Assert.Equal(11.0, r[0, 0]);
        Assert.Equal(12.0, r[0, 1]);
        Assert.Equal(13.0, r[1, 0]);
        Assert.Equal(14.0, r[1, 1]);
    }

    [Fact]
    public void Matrix_ScalarAddition_IsCommutative()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        Assert.Equal(m + 5.0, 5.0 + m);
    }

    [Fact]
    public void Matrix_Subtraction_IsCorrect()
    {
        var m1 = new Matrix(new double[,] { { 5.0, 6.0 }, { 7.0, 8.0 } });
        var m2 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var r  = m1 - m2;
        Assert.Equal(4.0, r[0, 0]);
        Assert.Equal(4.0, r[0, 1]);
    }

    [Fact]
    public void Matrix_ScalarMultiplication_IsCorrect()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var r = m * 2.0;
        Assert.Equal(2.0, r[0, 0]);
        Assert.Equal(4.0, r[0, 1]);
        Assert.Equal(6.0, r[1, 0]);
        Assert.Equal(8.0, r[1, 1]);
    }

    [Fact]
    public void Matrix_ScalarMultiplication_IsCommutative()
    {
        var m = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        Assert.Equal(m * 3.0, 3.0 * m);
    }

    [Fact]
    public void Matrix_MatrixMultiplication_IsCorrect()
    {
        // A = [[1,2],[3,4]]  B = [[5,6],[7,8]]  A*B = [[19,22],[43,50]]
        var A = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var B = new Matrix(new double[,] { { 5.0, 6.0 }, { 7.0, 8.0 } });
        var C = A * B;
        Assert.Equal(19.0, C[0, 0], precision: 12);
        Assert.Equal(22.0, C[0, 1], precision: 12);
        Assert.Equal(43.0, C[1, 0], precision: 12);
        Assert.Equal(50.0, C[1, 1], precision: 12);
    }

    [Fact]
    public void Matrix_MatrixMultiplication_ThrowsForInnerDimensionMismatch()
    {
        var A = new Matrix(2, 3);
        var B = new Matrix(2, 2);
        Assert.Throws<ArgumentException>(() => _ = A * B);
    }

    [Fact]
    public void Matrix_MatrixVectorMultiplication_IsCorrect()
    {
        // A = [[1,0],[0,2]]  v = [3, 4]  => [3, 8]
        var A = Matrix.Identity(2);
        A[1, 1] = 2.0;
        var v = new Vector([3.0, 4.0]);
        var r = A * v;
        Assert.Equal(3.0, r[0], precision: 12);
        Assert.Equal(8.0, r[1], precision: 12);
    }

    [Fact]
    public void Matrix_MatrixVectorMultiplication_ThrowsForDimensionMismatch()
    {
        var A = new Matrix(2, 3);
        var v = new Vector([1.0, 2.0]);
        Assert.Throws<ArgumentException>(() => _ = A * v);
    }

    [Fact]
    public void Matrix_ScalarDivision_IsCorrect()
    {
        var m = new Matrix(new double[,] { { 2.0, 4.0 }, { 6.0, 8.0 } });
        var r = m / 2.0;
        Assert.Equal(1.0, r[0, 0]);
        Assert.Equal(2.0, r[0, 1]);
    }

    [Fact]
    public void Matrix_ScalarDivision_ThrowsForZeroDivisor()
    {
        var m = new Matrix(2, 2);
        Assert.Throws<DivideByZeroException>(() => _ = m / 0.0);
    }
}

public class MatrixIdentityMultiplicationTests
{
    [Fact]
    public void Matrix_MultiplyByIdentity_ReturnsEquivalentMatrix()
    {
        var A = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var I = Matrix.Identity(2);
        var R = A * I;
        Assert.Equal(A, R);
    }
}

public class MatrixEqualityTests
{
    [Fact]
    public void Matrix_Equals_TrueForIdenticalValues()
    {
        var m1 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var m2 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        Assert.True(m1.Equals(m2));
        Assert.True(m1 == m2);
    }

    [Fact]
    public void Matrix_Equals_FalseForDifferentDimensions()
    {
        var m1 = new Matrix(2, 2);
        var m2 = new Matrix(2, 3);
        Assert.False(m1.Equals(m2));
    }

    [Fact]
    public void Matrix_Equals_FalseForDifferentValues()
    {
        var m1 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var m2 = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 5.0 } });
        Assert.True(m1 != m2);
    }
}

public class MatrixCloneTests
{
    [Fact]
    public void Matrix_Clone_IsDeepCopy()
    {
        var m    = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var copy = m.Clone();
        copy[0, 0] = 99.0;
        Assert.Equal(1.0, m[0, 0]);
    }

    [Fact]
    public void Matrix_ToArray_ReturnsIndependentCopy()
    {
        var m   = new Matrix(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });
        var arr = m.ToArray();
        arr[0, 0] = 99.0;
        Assert.Equal(1.0, m[0, 0]);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SparseMatrix tests
// ═════════════════════════════════════════════════════════════════════════════

public class SparseMatrixConstructorTests
{
    [Fact]
    public void SparseMatrix_Constructor_CreatesEmptyMatrix()
    {
        var s = new SparseMatrix(3, 4);
        Assert.Equal(3, s.Rows);
        Assert.Equal(4, s.Cols);
        Assert.Equal(0, s.NonZeroCount);
    }

    [Fact]
    public void SparseMatrix_Constructor_ThrowsForZeroDimension()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SparseMatrix(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SparseMatrix(3, 0));
    }

    [Fact]
    public void SparseMatrix_DenseArrayConstructor_StoresOnlyNonZeros()
    {
        double[,] arr = { { 1.0, 0.0, 2.0 }, { 0.0, 3.0, 0.0 } };
        var s = new SparseMatrix(arr);
        Assert.Equal(2, s.Rows);
        Assert.Equal(3, s.Cols);
        Assert.Equal(3, s.NonZeroCount);
        Assert.Equal(1.0, s[0, 0]);
        Assert.Equal(0.0, s[0, 1]);
        Assert.Equal(2.0, s[0, 2]);
        Assert.Equal(3.0, s[1, 1]);
    }

    [Fact]
    public void SparseMatrix_DenseArrayConstructor_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SparseMatrix((double[,])null!));
    }
}

public class SparseMatrixIdentityTests
{
    [Fact]
    public void SparseMatrix_Identity_HasOnesOnDiagonal()
    {
        var I = SparseMatrix.Identity(4);
        Assert.Equal(4, I.Rows);
        Assert.Equal(4, I.NonZeroCount);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(1.0, I[i, i]);
            if (i < 3) Assert.Equal(0.0, I[i, i + 1]);
        }
    }
}

public class SparseMatrixTridiagonalTests
{
    [Fact]
    public void SparseMatrix_Tridiagonal_HasCorrectStructure()
    {
        var main  = new Vector([4.0, 4.0, 4.0]);
        var upper = new Vector([1.0, 1.0]);
        var lower = new Vector([-1.0, -1.0]);
        var T = SparseMatrix.Tridiagonal(lower, main, upper);

        Assert.Equal(3, T.Rows);
        Assert.Equal(3, T.Cols);
        // Main diagonal
        Assert.Equal(4.0,  T[0, 0]);
        Assert.Equal(4.0,  T[1, 1]);
        Assert.Equal(4.0,  T[2, 2]);
        // Sub-diagonal
        Assert.Equal(-1.0, T[1, 0]);
        Assert.Equal(-1.0, T[2, 1]);
        // Super-diagonal
        Assert.Equal(1.0,  T[0, 1]);
        Assert.Equal(1.0,  T[1, 2]);
        // Off-tridiagonal are zero
        Assert.Equal(0.0,  T[0, 2]);
        Assert.Equal(0.0,  T[2, 0]);
    }

    [Fact]
    public void SparseMatrix_Tridiagonal_ThrowsForInconsistentBandLengths()
    {
        var main     = new Vector([1.0, 2.0, 3.0]);
        var tooShort = new Vector([1.0]);        // needs length 2
        var correct  = new Vector([1.0, 1.0]);

        Assert.Throws<ArgumentException>(
            () => SparseMatrix.Tridiagonal(tooShort, main, correct));
        Assert.Throws<ArgumentException>(
            () => SparseMatrix.Tridiagonal(correct, main, tooShort));
    }
}

public class SparseMatrixIndexerTests
{
    [Fact]
    public void SparseMatrix_Indexer_SetAndGet_WorkCorrectly()
    {
        var s = new SparseMatrix(3, 3);
        s[0, 0] = 5.0;
        s[2, 1] = -3.0;
        Assert.Equal(5.0,  s[0, 0]);
        Assert.Equal(0.0,  s[1, 1]);
        Assert.Equal(-3.0, s[2, 1]);
    }

    [Fact]
    public void SparseMatrix_Indexer_SettingZeroRemovesEntry()
    {
        var s = new SparseMatrix(3, 3);
        s[0, 0] = 5.0;
        Assert.Equal(1, s.NonZeroCount);
        s[0, 0] = 0.0;
        Assert.Equal(0, s.NonZeroCount);
        Assert.Equal(0.0, s[0, 0]);
    }

    [Fact]
    public void SparseMatrix_Indexer_ThrowsForOutOfRange()
    {
        var s = new SparseMatrix(3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = s[3, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = s[0, -1]);
    }
}

public class SparseMatrixMultiplyTests
{
    [Fact]
    public void SparseMatrix_Multiply_IdentityTimesVector_ReturnsVector()
    {
        var I = SparseMatrix.Identity(3);
        var v = new Vector([1.0, 2.0, 3.0]);
        var r = I.Multiply(v);
        Assert.Equal(1.0, r[0], precision: 12);
        Assert.Equal(2.0, r[1], precision: 12);
        Assert.Equal(3.0, r[2], precision: 12);
    }

    [Fact]
    public void SparseMatrix_Multiply_TridiagonalTimesVector_IsCorrect()
    {
        // T = [[2,-1,0],[-1,2,-1],[0,-1,2]]  v = [1,2,3]
        // Row 0: 2*1 + (-1)*2 + 0*3 = 0
        // Row 1: (-1)*1 + 2*2 + (-1)*3 = 0
        // Row 2: 0*1 + (-1)*2 + 2*3 = 4
        var main  = new Vector([2.0, 2.0, 2.0]);
        var off   = new Vector([-1.0, -1.0]);
        var T     = SparseMatrix.Tridiagonal(off, main, off);
        var v     = new Vector([1.0, 2.0, 3.0]);
        var r     = T * v;
        Assert.Equal(0.0, r[0], precision: 12);
        Assert.Equal(0.0, r[1], precision: 12);
        Assert.Equal(4.0, r[2], precision: 12);
    }

    [Fact]
    public void SparseMatrix_Multiply_ThrowsForDimensionMismatch()
    {
        var s = new SparseMatrix(3, 4);
        var v = new Vector([1.0, 2.0, 3.0]);
        Assert.Throws<ArgumentException>(() => s.Multiply(v));
    }
}

public class SparseMatrixConversionTests
{
    [Fact]
    public void SparseMatrix_ToArray_CorrectlyPopulatesZeros()
    {
        var s = new SparseMatrix(2, 2);
        s[0, 0] = 1.0;
        s[1, 1] = 2.0;
        double[,] arr = s.ToArray();
        Assert.Equal(1.0, arr[0, 0]);
        Assert.Equal(0.0, arr[0, 1]);
        Assert.Equal(0.0, arr[1, 0]);
        Assert.Equal(2.0, arr[1, 1]);
    }

    [Fact]
    public void SparseMatrix_ToDenseMatrix_EquivalentToToArray()
    {
        var s = new SparseMatrix(3, 3);
        s[0, 0] = 3.0;
        s[2, 2] = 7.0;
        var dense = s.ToDenseMatrix();
        Assert.Equal(s.Rows, dense.Rows);
        Assert.Equal(s.Cols, dense.Cols);
        Assert.Equal(3.0, dense[0, 0]);
        Assert.Equal(0.0, dense[0, 1]);
        Assert.Equal(7.0, dense[2, 2]);
    }
}

public class SparseMatrixCloneTests
{
    [Fact]
    public void SparseMatrix_Clone_IsDeepCopy()
    {
        var s = new SparseMatrix(3, 3);
        s[0, 0] = 5.0;
        var copy = s.Clone();
        copy[0, 0] = 99.0;
        Assert.Equal(5.0, s[0, 0]);
    }
}

public class SparseMatrixIsSquareTests
{
    [Fact]
    public void SparseMatrix_IsSquare_TrueForSquareMatrix()
    {
        Assert.True(new SparseMatrix(4, 4).IsSquare);
    }

    [Fact]
    public void SparseMatrix_IsSquare_FalseForRectangularMatrix()
    {
        Assert.False(new SparseMatrix(3, 5).IsSquare);
    }
}

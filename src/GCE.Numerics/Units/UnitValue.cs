using System.Globalization;

namespace GCE.Numerics;

/// <summary>
/// An immutable value type that pairs a numeric magnitude with its <see cref="UnitType"/>,
/// enabling unit-aware arithmetic, comparison, and conversion.
/// </summary>
/// <remarks>
/// <para>
/// Addition and subtraction require both operands to share the same
/// <see cref="UnitCategory"/>.  The right-hand value is automatically
/// converted to the left-hand unit before the operation; the result
/// carries the left-hand unit.
/// </para>
/// <para>
/// Multiplication and division by a plain <see langword="double"/> scalar
/// produce a result in the same unit.
/// </para>
/// <para>
/// Equality and ordering compare magnitudes normalised to the SI base unit
/// of each category (or Kelvin for temperature), so values in different but
/// compatible units are correctly ordered.
/// </para>
/// </remarks>
public readonly struct UnitValue : IEquatable<UnitValue>, IComparable<UnitValue>
{
    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>Gets the numeric magnitude in <see cref="Unit"/>.</summary>
    public double Value { get; }

    /// <summary>Gets the unit in which <see cref="Value"/> is expressed.</summary>
    public UnitType Unit { get; }

    /// <summary>
    /// Initialises a new <see cref="UnitValue"/> with the given magnitude and unit.
    /// </summary>
    public UnitValue(double value, UnitType unit)
    {
        Value = value;
        Unit  = unit;
    }

    // ── Conversion ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="UnitValue"/> expressing the same physical quantity
    /// in <paramref name="targetUnit"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="targetUnit"/> is in a different
    /// <see cref="UnitCategory"/> than <see cref="Unit"/>.
    /// </exception>
    public UnitValue ConvertTo(UnitType targetUnit) =>
        new(UnitConversions.Convert(Value, Unit, targetUnit), targetUnit);

    // ── Arithmetic operators ─────────────────────────────────────────────────

    /// <summary>
    /// Adds two <see cref="UnitValue"/> instances.
    /// The right operand is converted to the left operand's unit before addition.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operands are in incompatible unit categories.
    /// </exception>
    public static UnitValue operator +(UnitValue left, UnitValue right)
    {
        double rightConverted = UnitConversions.Convert(right.Value, right.Unit, left.Unit);
        return new UnitValue(left.Value + rightConverted, left.Unit);
    }

    /// <summary>
    /// Subtracts <paramref name="right"/> from <paramref name="left"/>.
    /// The right operand is converted to the left operand's unit before subtraction.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operands are in incompatible unit categories.
    /// </exception>
    public static UnitValue operator -(UnitValue left, UnitValue right)
    {
        double rightConverted = UnitConversions.Convert(right.Value, right.Unit, left.Unit);
        return new UnitValue(left.Value - rightConverted, left.Unit);
    }

    /// <summary>Returns the negation of <paramref name="operand"/>.</summary>
    public static UnitValue operator -(UnitValue operand) =>
        new(-operand.Value, operand.Unit);

    /// <summary>Scales <paramref name="unitValue"/> by a dimensionless <paramref name="scalar"/>.</summary>
    public static UnitValue operator *(UnitValue unitValue, double scalar) =>
        new(unitValue.Value * scalar, unitValue.Unit);

    /// <summary>Scales <paramref name="unitValue"/> by a dimensionless <paramref name="scalar"/>.</summary>
    public static UnitValue operator *(double scalar, UnitValue unitValue) =>
        new(unitValue.Value * scalar, unitValue.Unit);

    /// <summary>Divides <paramref name="unitValue"/> by a dimensionless <paramref name="divisor"/>.</summary>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="divisor"/> is zero.</exception>
    public static UnitValue operator /(UnitValue unitValue, double divisor)
    {
        if (divisor == 0.0)
            throw new DivideByZeroException("Cannot divide a UnitValue by zero.");
        return new(unitValue.Value / divisor, unitValue.Unit);
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(UnitValue other)
    {
        // Compare in base units to handle cross-unit equality
        double thisBase  = UnitConversions.ToBaseUnit(Value, Unit);
        double otherBase = UnitConversions.ToBaseUnit(other.Value, other.Unit);
        return thisBase.Equals(otherBase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is UnitValue other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        UnitConversions.ToBaseUnit(Value, Unit).GetHashCode();

    /// <summary>Returns <see langword="true"/> when both values represent the same quantity.</summary>
    public static bool operator ==(UnitValue left, UnitValue right) => left.Equals(right);

    /// <summary>Returns <see langword="true"/> when the values differ.</summary>
    public static bool operator !=(UnitValue left, UnitValue right) => !left.Equals(right);

    // ── Comparison ───────────────────────────────────────────────────────────

    /// <summary>
    /// Compares this instance to <paramref name="other"/> after normalising both
    /// to the SI base unit of their shared category.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the instances are in incompatible unit categories.
    /// </exception>
    public int CompareTo(UnitValue other)
    {
        UnitCategory thisCategory  = UnitConversions.GetCategory(Unit);
        UnitCategory otherCategory = UnitConversions.GetCategory(other.Unit);

        if (thisCategory != otherCategory)
            throw new InvalidOperationException(
                $"Cannot compare {Unit} ({thisCategory}) with {other.Unit} ({otherCategory}): " +
                "incompatible unit categories.");

        double thisBase  = UnitConversions.ToBaseUnit(Value, Unit);
        double otherBase = UnitConversions.ToBaseUnit(other.Value, other.Unit);
        return thisBase.CompareTo(otherBase);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    public static bool operator <(UnitValue left, UnitValue right) => left.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(UnitValue left, UnitValue right) => left.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is less than or equal to <paramref name="right"/>.</summary>
    public static bool operator <=(UnitValue left, UnitValue right) => left.CompareTo(right) <= 0;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</summary>
    public static bool operator >=(UnitValue left, UnitValue right) => left.CompareTo(right) >= 0;

    // ── Formatting ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a human-readable string such as <c>"25.00 Celsius"</c>.
    /// </summary>
    public override string ToString() =>
        $"{Value.ToString("G", CultureInfo.InvariantCulture)} {Unit}";
}

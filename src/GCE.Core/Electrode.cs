namespace GCE.Core;

/// <summary>
/// Concrete implementation of <see cref="IElectrode"/> describing a physical electrode.
/// </summary>
/// <param name="Material">The material this electrode is made from.</param>
/// <param name="Area">The electrochemically active surface area in m².</param>
public sealed record Electrode(IMaterial Material, double Area) : IElectrode;

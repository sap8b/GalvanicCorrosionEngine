namespace GCE.Simulation.Geometry;

internal static class GeometryCoordinateValidation
{
    public static void ValidateStrictlyIncreasing(double[] coordinates, string paramName)
    {
        if (coordinates.Length < 2)
            throw new ArgumentException("Coordinate array must contain at least two points.", paramName);

        for (int i = 1; i < coordinates.Length; i++)
        {
            if (coordinates[i] <= coordinates[i - 1])
                throw new ArgumentException("Coordinate array must be strictly increasing.", paramName);
        }
    }
}

using System.Text.Json;
using GCE.Core;
using GCE.IO;
using GCE.Simulation;

namespace GCE.Core.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file static class WriterFixtures
{
    public static SimulationResult TwoPointResult() => new()
    {
        TimePoints       = [0.0, 3600.0],
        MixedPotentials  = [-0.42, -0.41],
        CorrosionRates   = [0.1, 0.12],
    };

    public static SimulationResult EmptyResult() => new();

    public static GeometryMesh TinyMesh()
    {
        // 3×2 grid, nodes at (0, 0.5, 1) × (0, 1).
        var regions = new int[3, 2]
        {
            { 0, 0 },
            { -1, -1 },
            { 1, 1 },
        };
        return new GeometryMesh(
            XCoordinates: [0.0, 0.5, 1.0],
            YCoordinates: [0.0, 1.0],
            Regions: regions);
    }
}

// ── CsvResultWriter ───────────────────────────────────────────────────────────

public class CsvResultWriterTests
{
    [Fact]
    public void Write_ProducesHeaderAndTwoDataRows()
    {
        var writer = new CsvResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        string csv = sw.ToString();
        string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Time_s,MixedPotential_V,CorrosionRate_mmPerYear", lines[0].Trim());
        Assert.Equal(3, lines.Length); // header + 2 data rows
    }

    [Fact]
    public void Write_DataRow_ContainsExpectedValues()
    {
        var writer = new CsvResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        string[] lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string row2 = lines[2].Trim(); // second data row (t = 3600)

        Assert.Contains("3600", row2);
        Assert.Contains("-0.41", row2);
        Assert.Contains("0.12", row2);
    }

    [Fact]
    public void Write_EmptyResult_WritesOnlyHeader()
    {
        var writer = new CsvResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.EmptyResult(), sw);

        string[] lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }

    [Fact]
    public void Write_NullResult_Throws()
    {
        var writer = new CsvResultWriter();
        using var sw = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, sw));
    }

    [Fact]
    public void Write_NullWriter_Throws()
    {
        var writer = new CsvResultWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(WriterFixtures.TwoPointResult(), null!));
    }

    [Fact]
    public void WriteToFile_CreatesFileWithCorrectContent()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gce_csv_{Guid.NewGuid():N}.csv");
        try
        {
            new CsvResultWriter().WriteToFile(WriterFixtures.TwoPointResult(), path);
            string content = File.ReadAllText(path);
            Assert.Contains("Time_s", content);
            Assert.Contains("3600", content);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

// ── JsonResultWriter ──────────────────────────────────────────────────────────

public class JsonResultWriterTests
{
    [Fact]
    public void Write_ProducesValidJson()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        // Should not throw
        using var doc = JsonDocument.Parse(sw.ToString());
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }

    [Fact]
    public void Write_ContainsExpectedProperties()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        using var doc = JsonDocument.Parse(sw.ToString());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("timePoints_s",             out _));
        Assert.True(root.TryGetProperty("mixedPotentials_V",        out _));
        Assert.True(root.TryGetProperty("corrosionRates_mmPerYear", out _));
    }

    [Fact]
    public void Write_ArrayLengthsMatchResult()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();
        var result = WriterFixtures.TwoPointResult();

        writer.Write(result, sw);

        using var doc = JsonDocument.Parse(sw.ToString());
        var root = doc.RootElement;

        Assert.Equal(result.TimePoints.Count,      root.GetProperty("timePoints_s").GetArrayLength());
        Assert.Equal(result.MixedPotentials.Count, root.GetProperty("mixedPotentials_V").GetArrayLength());
        Assert.Equal(result.CorrosionRates.Count,  root.GetProperty("corrosionRates_mmPerYear").GetArrayLength());
    }

    [Fact]
    public void Write_EmptyResult_ProducesEmptyArrays()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.EmptyResult(), sw);

        using var doc = JsonDocument.Parse(sw.ToString());
        Assert.Equal(0, doc.RootElement.GetProperty("timePoints_s").GetArrayLength());
    }

    [Fact]
    public void Write_ConvergenceHistory_OmittedWhenEmpty()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        using var doc = JsonDocument.Parse(sw.ToString());
        // convergenceHistory should be absent when empty (WhenWritingNull)
        Assert.False(doc.RootElement.TryGetProperty("convergenceHistory", out _));
    }

    [Fact]
    public void Write_Compact_ProducesNoLeadingWhitespace()
    {
        var writer = new JsonResultWriter { Indented = false };
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        string json = sw.ToString();
        Assert.DoesNotContain('\n', json);
    }

    [Fact]
    public void Write_NullResult_Throws()
    {
        var writer = new JsonResultWriter();
        using var sw = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, sw));
    }

    [Fact]
    public void WriteToFile_CreatesFileWithValidJson()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gce_json_{Guid.NewGuid():N}.json");
        try
        {
            new JsonResultWriter().WriteToFile(WriterFixtures.TwoPointResult(), path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

// ── VtkResultWriter ───────────────────────────────────────────────────────────

public class VtkResultWriterTests
{
    [Fact]
    public void Write_ProducesValidXmlWithVtkFileRoot()
    {
        var writer = new VtkResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        string xml = sw.ToString();
        Assert.StartsWith("<?xml", xml);
        Assert.Contains("<VTKFile", xml);
        Assert.Contains("</VTKFile>", xml);
    }

    [Fact]
    public void Write_ContainsRectilinearGridElement()
    {
        var writer = new VtkResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        Assert.Contains("RectilinearGrid", sw.ToString());
    }

    [Fact]
    public void Write_FieldDataContainsTimeSeriesArrays()
    {
        var writer = new VtkResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        string xml = sw.ToString();
        Assert.Contains("Time_s",                   xml);
        Assert.Contains("MixedPotential_V",          xml);
        Assert.Contains("CorrosionRate_mmPerYear",   xml);
    }

    [Fact]
    public void Write_WithMesh_ContainsRegionIdArray()
    {
        var writer = new VtkResultWriter(WriterFixtures.TinyMesh());
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        Assert.Contains("RegionId", sw.ToString());
    }

    [Fact]
    public void Write_WithMesh_ExtentReflectsMeshSize()
    {
        var mesh = WriterFixtures.TinyMesh(); // 3×2
        var writer = new VtkResultWriter(mesh);
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.TwoPointResult(), sw);

        // WholeExtent should be "0 2 0 1 0 0" for a 3×2 grid.
        Assert.Contains("WholeExtent=\"0 2 0 1 0 0\"", sw.ToString());
    }

    [Fact]
    public void Write_EmptyResult_StillProducesValidVtk()
    {
        var writer = new VtkResultWriter();
        using var sw = new StringWriter();

        writer.Write(WriterFixtures.EmptyResult(), sw);

        string xml = sw.ToString();
        Assert.Contains("<VTKFile", xml);
        Assert.Contains("</VTKFile>", xml);
    }

    [Fact]
    public void Write_NullResult_Throws()
    {
        var writer = new VtkResultWriter();
        using var sw = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, sw));
    }

    [Fact]
    public void Write_NullWriter_Throws()
    {
        var writer = new VtkResultWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(WriterFixtures.TwoPointResult(), null!));
    }

    [Fact]
    public void Constructor_NullMesh_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VtkResultWriter(null!));
    }

    [Fact]
    public void WriteToFile_CreatesFileWithVtkContent()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gce_vtk_{Guid.NewGuid():N}.vtr");
        try
        {
            new VtkResultWriter(WriterFixtures.TinyMesh())
                .WriteToFile(WriterFixtures.TwoPointResult(), path);

            string content = File.ReadAllText(path);
            Assert.Contains("<VTKFile", content);
            Assert.Contains("RegionId", content);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

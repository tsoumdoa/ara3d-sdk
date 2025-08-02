using Ara3D.IO.StepParser;
using Ara3D.Logging;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public static class IfcRelationTests
{
    [Test]
    public static void TestOutputGraph()
    {
        using var doc = new StepDocument(IfcGeometryTests.InputFile, null);
        var graph = new StepGraph(doc);
        var d = graph.ComputeAttributes();
        foreach (var kv in d.OrderBy(kv2 => kv2.Key))
        {
            Console.WriteLine($"{kv.Key}[{kv.Value.Count}] = {kv.Value}");
        }
    }

    [Test]
    public static void TestOutputRelations()
    {
        var logger = Logger.Console;
        using var doc = new StepDocument(IfcGeometryTests.InputFile, logger);
        logger.Log($"Loaded {doc.FilePath.GetFileName()}");
        var cnt = 0;
        var graph = new StepGraph(doc);
        logger.Log($"Graph has {graph.Definitions.Count} definitions and {graph.Relations.Count} relations");

        Console.WriteLine($"=========");
        Console.WriteLine($"RELATIONS");
        Console.WriteLine($"=========");
        OutputRelations(graph, graph.Relations);
        Console.WriteLine();

        Console.WriteLine($"=================");
        Console.WriteLine($"INVERSE RELATIONS");
        Console.WriteLine($"=================");
        OutputRelations(graph, graph.InverseRelations);
    }

    public static void OutputRelations(StepGraph graph, MultiDictionary<UInt128, UInt128> relations)
    {
        var nameRelations = new Dictionary<string, HashSet<string>>();
        foreach (var rel in relations)
        {
            var defId = rel.Key;
            var defName = graph.GetEntityName(defId);
            if (!nameRelations.TryGetValue(defName, out var set))
            {
                set = new HashSet<string>();
                nameRelations.Add(defName, set);
            }
            foreach (var id in rel.Value)
            {
                var name = graph.GetEntityName(id);
                set.Add(name);
            }
        }

        foreach (var kv in nameRelations.OrderBy(_kv => _kv.Key))
        {
            var defName = kv.Key;
            Console.WriteLine($"Entity {defName} has relations");
            var vals = kv.Value.OrderBy(x => x).ToList();
            foreach (var val in vals)
                Console.WriteLine($"  - {val}");
        }
    }
}
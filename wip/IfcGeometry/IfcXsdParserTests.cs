using System.Xml.Linq;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public static class IfcXsdParserTests
{
    public static FilePath IfcSchemaFile => PathUtil.GetCallerSourceFolder().RelativeFile("IFC4.xsd");
    public static string IfcSchemaText => IfcSchemaFile.ReadAllText();
    public static XDocument IfcSchema => XDocument.Parse(IfcSchemaText);

    public static string XElementToString(XElement e)
    {
        return e.Name.LocalName + "(" + e.Elements().Select(e1 => e1.Name.LocalName).JoinStringsWithComma() + ")";
    }

    public static IEnumerable<string> GetRootChildElementNames(XDocument doc)
        => doc.Root.Elements().Select(XElementToString);

    [Test]
    public static void ListDistinctKindsOfTypes()
    {
        var set = GetRootChildElementNames(IfcSchema);
        var sorted = set.Distinct().OrderBy(s => s).ToList();
        foreach (var x in sorted)
            Console.WriteLine(x);
    }

    [Test]
    public static void ListElements()
    {
        var xs = IfcSchema
            .Root
            .Elements()
            .Where(e => e.Name.LocalName == "element")
            .Select(e => $"{e.Attribute("name")?.Value}:{e.Attribute("type")?.Value}")
            .OrderBy(x => x);
        foreach (var x in xs)
        {
            Console.WriteLine($"{x}");
        }
    }

    [Test]
    public static void ListElementsThatAreNotNillable()
    {
        var xs = IfcSchema
            .Root
            .Elements()
            .Where(e => e.Name.LocalName == "element" && e.Attribute("nillable")?.Value != "true")
            .Select(e => e.Attribute("name")?.Value)
            .OrderBy(x => x);
        foreach (var x in xs)
        {
            Console.WriteLine($"{x}");  
        }
    }

    private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public static void TestGenerateTypes()
    {
        var xsd = IfcSchema;

        var allTypes = xsd.Root!.Elements(xs + "simpleType")
            .Concat(xsd.Root!.Elements(xs + "complexType"));

        foreach (var t in allTypes)
        {
            var generated = IfcXsdToCSharp.GenerateCSharp(t); 
            Console.WriteLine(generated);
        }
    }
}
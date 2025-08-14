using System.Xml.Linq;
using Ara3D.Utils;

namespace Ara3D.IfcGeometry;

public static class IfcXsdParserTests
{
    public static FilePath IfcSchemaFile => PathUtil.GetCallerSourceFolder().RelativeFile("IFC4.xsd");
    public static string IfcSchemaText => IfcSchemaFile.ReadAllText();
    public static XDocument IfcSchema => XDocument.Parse(IfcSchemaText);

    public static Dictionary<string, XElement> Attributes => GetXElementLookup("attribute");
    public static Dictionary<string, XElement> AttributeGroups => GetXElementLookup("attributeGroup");
    public static Dictionary<string, XElement> Elements => GetXElementLookup("element");
    public static Dictionary<string, XElement> ComplexTypes => GetXElementLookup("complexType");
    public static Dictionary<string, XElement> Group => GetXElementLookup("group");
    public static Dictionary<string, XElement> SimpleType => GetXElementLookup("simpleType");

    public static Dictionary<string, XElement> GetXElementLookup(string localName) => IfcSchema.Root.Elements().Where(e => e.Name.LocalName == localName).ToDictionary(e => e.Attribute("name")?.Value ?? "", e => e);
    
    public static string XElementToString(XElement e)
    {
        return e.Name.LocalName + "(" + e.Elements().Select(e1 => e1.Name.LocalName).JoinStringsWithComma() + ")";
    }

    public static IEnumerable<string> GetRootChildElementNames(XDocument doc)
        => doc.Root.Elements().Select(XElementToString);

    [Test]
    public static void TestLookups()
    {
        TestLookup(Attributes, nameof(Attributes));
        TestLookup(AttributeGroups, nameof(AttributeGroups));
        TestLookup(Elements, nameof(Elements));
        TestLookup(ComplexTypes, nameof(ComplexTypes));
        TestLookup(AttributeGroups, nameof(AttributeGroups));
        TestLookup(Group, nameof(Group));
        TestLookup(SimpleType, nameof(SimpleType));
    }

    public static void TestLookup(Dictionary<string, XElement> d, string name)
    {
        Console.WriteLine($"{name} has {d.Count} elements");
        foreach (var kv in d)
        {
            Console.WriteLine($"  {kv.Key}");        
        }
    }

    [Test]
    public static void ListDistinctKindsOfTypes()
    {
        var set = GetRootChildElementNames(IfcSchema);
        var sorted = set.Distinct().OrderBy(s => s).ToList();
        foreach (var x in sorted)
            Console.WriteLine(x);
    }

    public static IEnumerable<XElement> GetIfcElements()
        => Elements.Values;

    [Test]
    public static void ListElements()
    {
        var xs = GetIfcElements().Select(e => $"{e.Attribute("name")?.Value}:{e.Attribute("type")?.Value}")
            .OrderBy(x => x);
        foreach (var x in xs)
        {
            Console.WriteLine($"{x}");
        }
    }

    [Test]
    public static void ListElementsThatAreNotNillable()
    {
        var xs = GetIfcElements()
            .Where(e => e.Attribute("nillable")?.Value != "true")
            .Select(e => e.Attribute("name")?.Value)
            .OrderBy(x => x);
        foreach (var x in xs)
        {
            Console.WriteLine($"{x}");  
        }
    }

    public static readonly XNamespace XsNs = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public static void TestGenerateTypes()
    {
        var xsd = IfcSchema;

        var allTypes = xsd.Root!.Elements(XsNs + "simpleType")
            .Concat(xsd.Root!.Elements(XsNs + "complexType"));

        foreach (var t in allTypes)
        {
            var generated = IfcXsdToCSharp.GenerateCSharp(t); 
            Console.WriteLine(generated);
        }
    }
}
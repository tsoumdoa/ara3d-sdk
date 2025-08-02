using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Ara3D.IfcGeometry;

public static class IfcXsdToCSharp
{
    public static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";

    public static string GenerateCSharp(XElement typeElement, string ns = "Ifc")
    {
        if (typeElement is null) throw new ArgumentNullException(nameof(typeElement));

        var name = typeElement.Attribute("name")?.Value
                   ?? throw new ArgumentException("Unnamed XSD type.");

        var sb = new StringBuilder();

        if (IsSimple(typeElement))
            AppendSimpleType(sb, typeElement, name);
        else if (IsComplex(typeElement))
            AppendComplexType(sb, typeElement, name);
        else
            throw new ArgumentException($"Unsupported node: {typeElement.Name}");

        return sb.ToString();
    }

    // ────────────────────────────── simple types ─────────────────────────────
    public static void AppendSimpleType(StringBuilder sb, XElement st, string name)
    {
        AppendDoc(sb, st);
        if (IsEnumeration(st))
            AppendEnum(sb, st, name);
        else
            AppendScalarWrapper(sb, st, name);
    }

    public static bool IsEnumeration(XElement st)
        => st.Element(xs + "restriction")?.Elements(xs + "enumeration").Any() == true;

    public static void AppendEnum(StringBuilder sb, XElement st, string name)
    {
        sb.AppendLine($"    public enum {name}");
        sb.AppendLine("    {");
        foreach (var e in st.Element(xs + "restriction").Elements(xs + "enumeration"))
            sb.AppendLine($"        {EnumLiteral(e)},");
        sb.AppendLine("    }");
    }

    public static void AppendScalarWrapper(StringBuilder sb, XElement st, string name)
    {
        var baseCs = MapXsdToCSharp(st.Element(xs + "restriction")?.Attribute("base")?.Value ?? "xs:string");
        sb.AppendLine($"    public readonly record struct {name}({baseCs} Value);");
    }

    // ───────────────────────────── complex types ─────────────────────────────
    public static void AppendComplexType(StringBuilder sb, XElement ct, string name)
    {
        var (derived, body) = SplitInheritance(ct);
        AppendDoc(sb, ct);

        sb.AppendLine($"    public partial class {name}{derived}");
        sb.AppendLine("    {");

        CollectAttributes(body).ForEach(a => AppendAttribute(sb, a));
        CollectParticles(body).ForEach(p => AppendParticle(sb, p));

        sb.AppendLine("    }");
    }

    public static (string derived, XElement body) SplitInheritance(XElement ct)
    {
        var cc = ct.Element(xs + "complexContent");
        if (cc is null) return (string.Empty, ct);

        var ext = cc.Element(xs + "extension");
        var res = cc.Element(xs + "restriction");
        var baseT = MapXsdToCSharp(ext?.Attribute("base")?.Value ?? res?.Attribute("base")?.Value);
        var clause = string.IsNullOrEmpty(baseT) ? string.Empty : $" : {baseT}";
        return (clause, ext ?? res ?? ct);
    }

    // ───────────────────────── attributes ────────────────────────────────────
    public static List<XElement> CollectAttributes(XElement ct)
        => ct.Elements(xs + "attribute")
             .Concat(ResolveGroups(ct, "attributeGroup").Descendants(xs + "attribute"))
             .ToList();

    public static void AppendAttribute(StringBuilder sb, XElement attr)
    {
        AppendDoc(sb, attr);

        var name = ToPascal(attr.Attribute("name")?.Value);
        var type = MapXsdToCSharp(attr.Attribute("type")?.Value);
        var required = attr.Attribute("use")?.Value == "required";

        if (attr.Attribute("fixed") is not null)
        {
            sb.AppendLine($"        public const {type} {name} = {Literal(type, attr.Attribute("fixed").Value)};");
            return;
        }

        var defaultVal = attr.Attribute("default")?.Value;
        var defaultInit = defaultVal is null ? string.Empty : $" = {Literal(type, defaultVal)};";
        var nullable = required ? string.Empty : "?";
        var requiredAttr = required ? "[Required] " : string.Empty;

        sb.AppendLine($"        {requiredAttr}public {type}{nullable} {name} {{ get; set; }}{defaultInit}");
    }

    // ───────────────────────── particles (elements / choice) ────────────────
    public static List<XElement> CollectParticles(XElement ct)
        => ct.Elements(xs + "element").ToList();
        /*
             .Concat(ResolveGroups(ct, "group").Descendants(xs + "element"))
             .Concat(ct.Elements(xs + "choice"))
             .ToList();
        */

    public static void AppendChoice(StringBuilder sb, XElement p)
    {
        var types = p.Elements(xs + "element").Select(e => MapXsdToCSharp(e.Attribute("type")?.Value)).Distinct();
        var prop = ToPascal(p.Attribute("name")?.Value ?? "OneOf");
        sb.AppendLine($"        public object? {prop} {{ get; set; }} // {string.Join(", ", types)}");
    }

    public static void AppendParticle(StringBuilder sb, XElement p)
    {
        if (IsChoice(p))
        {
            AppendChoice(sb, p);
        }
        else
        {
            AppendDoc(sb, p);
            var prop = ToPascal(p.Attribute("name")?.Value);
            var type = MapXsdToCSharp(p.Attribute("type")?.Value ?? InferAnonymousElementType(p));
            if (IsCollection(p)) type = $"List<{type}>";
            sb.AppendLine($"        public {type} {prop} {{ get; set; }}");
        }
    }

    // ──────────────────────────── helpers ────────────────────────────────────
    public static StringBuilder StartFile(string ns)
        => new StringBuilder()
            .AppendLine("using System;")
            .AppendLine("using System.Collections.Generic;")
            .AppendLine("using System.ComponentModel.DataAnnotations;")
            .AppendLine()
            .AppendLine($"namespace {ns}")
            .AppendLine("{");

    public static void EndFile(StringBuilder sb) => sb.AppendLine("}");

    public static void AppendDoc(StringBuilder sb, XElement el)
    {
        var doc = el.Element(xs + "annotation")?.Element(xs + "documentation")?.Value?.Trim();
        if (doc is not null) sb.AppendLine($"    /// <summary>{doc}</summary>");
    }

    public static IEnumerable<XElement> ResolveGroups(XElement ct, string groupNode)
        => ct.Elements(xs + groupNode)
             .Select(g => ResolveGlobal(ct.Document, g.Attribute("ref")?.Value, xs + groupNode))
             .Where(d => d is not null);

    public static XElement? ResolveGlobal(XDocument doc, string? q, XName node)
    {
        if (q is null) return null;
        var local = q.Split(':').Last();
        return doc.Root?.Elements(node)
            .FirstOrDefault(e => e.Attribute("name")?.Value == local);
    }

    public static bool IsSimple(XElement el) => el.Name == xs + "simpleType";
    public static bool IsComplex(XElement el) => el.Name == xs + "complexType";
    public static bool IsChoice(XElement el) => el.Name == xs + "choice";

    public static bool IsCollection(XElement el)
        => el.Attribute("maxOccurs")?.Value switch
        {
            "unbounded" => true,
            var v when int.TryParse(v, out var n) && n > 1 => true,
            _ => false
        };

    public static string MapXsdToCSharp(string? x)
    {
        if (x is null) return "string";
        var t = x.Split(':').Last().Replace("-wrapper", string.Empty);
        return t switch
        {
            "string" => "string",
            "boolean" => "bool",
            "int" or "integer" => "int",
            "long" => "long",
            "double" => "double",
            "decimal" => "decimal",
            "dateTime" => "DateTime",
            _ => t
        };
    }

    public static string EnumLiteral(XElement e)
        => SanitizeEnum(e.Attribute("value")?.Value);

    public static string SanitizeEnum(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return "Undefined";
        var s = Regex.Replace(v.Trim('.', '_'), @"[^A-Za-z0-9]", "");
        s = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
        return char.IsDigit(s[0]) ? "_" + s : s;
    }

    public static string ToPascal(string? n)
        => string.IsNullOrEmpty(n) ? "Unnamed" : char.ToUpperInvariant(n[0]) + n[1..];

    public static string Literal(string t, string v)
        => t switch
        {
            "string" => $"\"{v}\"",
            "bool" => v.ToLowerInvariant(),
            "double" => v + "d",
            "decimal" => v + "m",
            _ => v
        };

    private static string InferAnonymousElementType(XElement el)
    {
        var inner = el.Element(xs + "complexType")?
            .Element(xs + "sequence")?
            .Element(xs + "element");

        if (inner is null) return "string";

        var raw = inner.Attribute("type")?.Value
                  ?? inner.Attribute("ref")?.Value
                  ?? "xs:string";

        var mapped = MapXsdToCSharp(raw);
        return IsCollection(inner) ? $"List<{mapped}>" : mapped;
    }
}

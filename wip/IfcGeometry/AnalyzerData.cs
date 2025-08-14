namespace Ara3D.IfcGeometry;

public class IfcAnalyzerNode
{
    public string color;
    public int count;
}

public class IfcAnalyzerRelation
{
    public string name;
    public int count;
}

public class IfcAnalyzerData
{
    public Dictionary<string, IfcAnalyzerNode> nodes = new();
    public Dictionary<string, List<IfcAnalyzerRelation>> relations = new();
}
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Ara3D.Bowerbird.RevitSamples;

public class BimOpenSchemaExportContext : IExportContext
{
    public Stack<Transform> Transforms { get; set; }
    public Stack<Document> Documents { get; set; }

    public Transform CurrentTransform => Transforms.Peek();
    public Document CurrentDocument => Documents.Peek();

    public void Create()
    {

    }

    public bool Start()
    {
        return true;
    }

    public void Finish()
    {
    }

    public bool IsCanceled()
    {
        return false;
    }

    public RenderNodeAction OnViewBegin(ViewNode node)
    {
        return RenderNodeAction.Proceed;
    }

    public void OnViewEnd(ElementId elementId)
    {
    }

    public RenderNodeAction OnElementBegin(ElementId elementId)
    {
        // TODO: 
        return RenderNodeAction.Proceed;
    }

    public void OnElementEnd(ElementId elementId)
    {
    }

    public RenderNodeAction OnInstanceBegin(InstanceNode node)
    {
        var curTransform = node.GetTransform();
        var newTransform = CurrentTransform.Multiply(curTransform);
        Transforms.Push(newTransform);
        return RenderNodeAction.Proceed;
    }

    public void OnInstanceEnd(InstanceNode node)
    {
        Transforms.Pop();
    }

    public RenderNodeAction OnLinkBegin(LinkNode node)
    {
        var newDoc = node.GetDocument();
        Documents.Push(newDoc);
        Transforms.Push(node.GetTransform());
        return RenderNodeAction.Proceed;
    }

    public void OnLinkEnd(LinkNode node)
    {
        Documents.Pop();
        Transforms.Pop();
    }

    public RenderNodeAction OnFaceBegin(FaceNode node)
    {
        var tmp = node.GetFace();
        return RenderNodeAction.Proceed;
    }

    public void OnFaceEnd(FaceNode node)
    {
    }

    public void OnRPC(RPCNode node)
    {
        // var tmp = node.
    }

    public void OnLight(LightNode node)
    {
    }

    public void OnMaterial(MaterialNode node)
    {
        // TODO:
    }

    public void OnPolymesh(PolymeshTopology node)
    {
        var points = node.GetPoints();
        var facets = node.GetFacets();
    }
}
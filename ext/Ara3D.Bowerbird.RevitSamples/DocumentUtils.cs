using System;
using System.Collections.Generic;
using System.Linq;
using Ara3D.Utils;
using Autodesk.Revit.DB;

namespace Ara3D.Bowerbird.RevitSamples;

public static class DocumentUtils
{
    public static IEnumerable<Document> GetLinkedDocuments(this Document doc)
        => new FilteredElementCollector(doc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Select(li => li.GetLinkDocument())
            .WhereNotNull();

    public static IEnumerable<Element> GetElementsThatAreNotTypes(this Document doc)
        => new FilteredElementCollector(doc).WhereElementIsNotElementType();

    public static IEnumerable<Element> GetElementsThatAreTypes(this Document doc)
        => new FilteredElementCollector(doc).WhereElementIsElementType();
}
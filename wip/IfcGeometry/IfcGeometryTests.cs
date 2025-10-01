using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using System.Text;
using Ara3D.DataTable;
using Ara3D.IO.StepParser;
using Ara3D.Logging;
using Ara3D.Utils;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Ara3D.IfcGeometry
{
    public static class IfcGeometryTests
    {
        public static string GeometryEntities = @"IFCARBITRARYCLOSEDPROFILEDEF
IFCAXIS2PLACEMENT2D
IFCBOOLEANCLIPPINGRESULT
IFCBOUNDINGBOX
IFCPOLYLINE
IFCCARTESIANPOINT
IFCCARTESIANTRANSFORMATIONOPERATOR3D
IFCCLOSEDSHELL
IFCCOLOURRGB
IFCCOMPOSITECURVE
IFCCOMPOSITECURVESEGMENT
IFCCURVEBOUNDEDPLANE
IFCDIRECTION
IFCFACE
IFCFACEBOUND
IFCFACEOUTERBOUND
IFCFACETEDBREP
IFCGEOMETRICCURVESET
IFCHALFSPACESOLID
IFCPLANE
IFCPOLYGONALBOUNDEDHALFSPACE
IFCPOLYLINE
IFCPOLYLOOP
";

        public static FilePath InputFile =
            @"C:\Users\cdigg\data\Ara3D\impraria\0000100120-093 - OXAGON ADVANCED HEALTH CENTER\STAGE 3A - CONCEPT DESIGN\ARC\03-730000-0000100120-DAH-ARC-MDL-000009 _IFC_D.ifc";
            //PathUtil.GetCallerSourceFolder().RelativeFile("..", "..", "data", "AC20-FZK-Haus.ifc");
            //@"C:\Users\cdigg\data\Ara3D\snapshot-data\ifc\long_running\B11ALL.ifc";

        public readonly struct StepRelation
        {
            public readonly StepToken SrcId;
            public readonly StepToken DestId;
            public StepRelation(StepToken srcId, StepToken destId)
            {
                SrcId = srcId;
                DestId = destId;
            }
        }

        public static void AddRelations(StepDocument doc, Dictionary<StepToken, string> d, StepToken srcId, List<StepRelation> relations, StepRawValue val)
        {
            if (val.IsId)
            {
                var destId = doc.RawValueData.AsToken(val);
                relations.Add(new(srcId, destId));
            }
            else if (val.IsList)
            {
                var attrs = doc.RawValueData.AsArray(val);
                foreach (var attr in attrs)
                    AddRelations(doc, d, srcId, relations, attr);
            }
        }
        
        public static List<StepRelation> GetRelations(StepDocument doc, Dictionary<StepToken, string> d)
        {
            var r = new List<StepRelation>();
            foreach (var def in doc.Definitions)
            {
                var srcId = def.IdToken;
                var val = doc.RawValueData.GetEntityAttributesValue(def);
                AddRelations(doc, d, srcId, r, val);
            }
            return r;
        }

        [Test]
        public static void OutputRelations()
        {
            var logger = Logger.Console;

            var f = InputFile;
            var fp = new FilePath(f);
            logger.Log($"Loading {fp.GetFileName()}");
            Console.WriteLine($"Input file is: {fp.GetFileSize():N0} bytes");
            using var doc = new StepDocument(f, logger);
            logger.Log($"Completed loading and initial parsing");

            var d = doc.GetEntityNameLookup();
            logger.Log($"Computed entity name lookup with {d.Count:N0} entries");

            var relations = GetRelations(doc, d);

            logger.Log($"Found {relations.Count:N0} relations");

            var data = new IfcAnalyzerData();
            var nodeGroups = doc.Definitions.GroupBy(doc.RawValueData.GetEntityName).ToList();
            foreach (var group in nodeGroups)
            {
                var node = new IfcAnalyzerNode() { color = RandomHighContrastHex(), count = group.Count() };
                data.nodes.Add(group.Key, node);
            }

            logger.Log($"Computed {nodeGroups.Count:N0} node groups");
            
            var relationGroups = relations.GroupBy(r => d[r.SrcId]).ToList();

            logger.Log($"Computed {relationGroups.Count:N0} relation groups");

            foreach (var group in relationGroups)
            {
                var srcName = group.Key;
                if (srcName == "IFCOWNERHISTORY")
                    continue;
                foreach (var group2 in group.GroupBy(r => d[r.DestId]))
                {
                    if (group2.Key == "IFCOWNERHISTORY")
                        continue;

                    if (!data.relations.ContainsKey(srcName))
                    {
                        var list = new List<IfcAnalyzerRelation>();
                        data.relations.Add(srcName, list);
                    }
                    var relation = new IfcAnalyzerRelation()
                    {
                        name = group2.Key,
                        count = group2.Count()
                    };
                    data.relations[srcName].Add(relation);
                }
            }

            logger.Log($"Added the relations");

            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                IncludeFields = true
            });
            logger.Log($"Computed json with {json.Length:N0} characters");

            var outputFile = PathUtil.GetCallerSourceFolder().RelativeFile("graph-data.js");
            outputFile.WriteAllText("window.data = " + json);
            logger.Log($"Wrote data to {outputFile} with {outputFile.GetFileSize():N0} bytes");
        }

        public static HashSet<string> GetLocalTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Select(t => t.Name.ToUpperInvariant())
                .Where(n => n.StartsWith("IFC")).ToHashSet();
        }

        [Test]
        public static void TestLoadTimes()
        {
            //var dir = new DirectoryPath(@"C:\Users\cdigg\data\Ara3D\impraria\0000100120-093 - OXAGON ADVANCED HEALTH CENTER\STAGE 3A - CONCEPT DESIGN");
            var dir = new DirectoryPath(@"C:\Users\cdigg\data\Ara3D\impraria");
            var files = dir.GetFiles("*.ifc", true).ToList();
            var logger = Logger.Console;
            var totalSize = 0L;
            foreach (var filePath in files)
            {
                logger.Log($"Loading {filePath.GetFileName()} which is {filePath.GetFileSizeAsString()}");
                totalSize += filePath.GetFileSize();
                using var doc = StepDocument.Create(filePath);
                logger.Log($".. completed");
                logger.Log($"Found {doc.Definitions.Count} ID definitions");
                var names = doc.Definitions.Select(doc.RawValueData.GetEntityName).Distinct().ToList();
                logger.Log($"Counted {names.Count} distinct entity names");
            }

            logger.Log($"Loaded {files.Count} files with a total of {PathUtil.BytesToString(totalSize)}");
        }

        [Test]
        public static void TestEcho()
        {
            var logger = Logger.Console;
            using var doc = new StepDocument(InputFile, logger);
            logger.Log($"Loaded {doc.FilePath.GetFileName()}");
            foreach (var def in doc.Definitions)
            {
                Console.WriteLine($"{def.IdToken}= {doc.RawValueData.ToString(def)}");
            }
        }

        public static Vector3 AsPoint(this StepRawValueData data, StepRawValue val)
        {
            var vals = data.AsNumbers(val);
            return new Vector3(
                vals.Length > 0 ? (float)vals[0] : 0f,
                vals.Length > 1 ? (float)vals[1] : 0f,
                vals.Length > 2 ? (float)vals[2] : 0f);
        }

        /// <summary>
        /// Interprets the low‑to‑high bytes of <paramref name="value"/> as an ASCII
        /// string.  Scans until the first 0‑byte or 16 bytes, whichever comes first.
        /// </summary>
        public static string ToAsciiString(UInt128 value)
        {
            // Fast path: almost always you have a short token (<=16 B),
            // so we use a stack‑allocated buffer and Span.
            Span<byte> tmp = stackalloc byte[16];

            // Write the UInt128 into the span *in native endianness*.
            // This copies 16 bytes with one move (the JIT unrolls it).
            BinaryPrimitives.WriteUInt128LittleEndian(tmp, value);

            // Find '\0'
            int len = 0;
            // Unrolled loop is fastest for ≤16 iterations.
            for (; len < 16; ++len)
            {
                if (tmp[len] == 0)
                    break;
            }

            // Convert the slice directly to a string without an intermediate array.
            return Encoding.ASCII.GetString(tmp[..len]);
        }

        [Test]
        public static void CommonStepEntities()
        {
            var logger = Logger.Console;
            logger.Log($"Loading {InputFile.GetFileName()} which has {InputFile.GetFileSize():N0} bytes");
            using var doc = new StepDocument(InputFile, logger);
            var data = doc.RawValueData;
            var groups0 = doc.Definitions.GroupBy(data.GetEntityName);
            var groups1 = groups0.Select(g => (g.Key, g.ToList())).OrderByDescending(c => c.Item2.Count);
            foreach (var g in groups1)
            {
                Console.WriteLine($"{g.Item1} - {g.Item2.Count}");
            }
        }

        [Test]
        public static unsafe void TestIfcBin()
        {
            var logger = Logger.Console;
            logger.Log($"Loading {InputFile.GetFileName()} which has {InputFile.GetFileSize():N0} bytes");
            using var doc = new StepDocument(InputFile, logger);
            var ifcBin = IfcBin.Create(doc);
            logger.Log($"Created IFC-Bin");

            logger.Log($"{ifcBin.DefinitionIds.Count:N0} definition ids");
            logger.Log($"{ifcBin.DefinitionEntities.Count:N0} definition entities");
            logger.Log($"{ifcBin.DefinitionVars.Count:N0} definition vars");

            logger.Log($"{ifcBin.Strings.Count:N0} strings");
            logger.Log($"{ifcBin.Ids.Count:N0} ids");
            logger.Log($"{ifcBin.Numbers.Count:N0} numbers");
            logger.Log($"{ifcBin.EntityNames.Count:N0} entity names");
            logger.Log($"{ifcBin.Symbols.Count:N0} symbols");

            logger.Log($"{ifcBin.StringListValues.Count:N0} string list values");
            logger.Log($"{ifcBin.IdListValues.Count:N0} id list values");
            logger.Log($"{ifcBin.NumberListValues.Count:N0} number list values");
            logger.Log($"{ifcBin.SymbolListValues.Count:N0} symbol list values");
            logger.Log($"{ifcBin.EntityNameListValues.Count:N0} entity name list values");
            logger.Log($"{ifcBin.Lists.Count:N0} lists");
            logger.Log($"{ifcBin.Vars.Count:N0} vars");

            var size = 0L;
            size += ifcBin.DefinitionIds.Count * sizeof(int);
            //size += ifcBin.DefinitionEntities.Count * sizeof(short);
            //size += ifcBin.DefinitionVars.Count * sizeof(int);
            size += ifcBin.Strings.Sum(s => s.Length + 1);
            //size += ifcBin.Ids.Count * sizeof(int);
            size += ifcBin.Numbers.Count * sizeof(double);
            size += ifcBin.EntityNames.Sum(s => s.Length + 1);
            size += ifcBin.Symbols.Sum(s => s.Length + 1);
            size += ifcBin.StringListValues.Count * sizeof(short);
            size += ifcBin.IdListValues.Count * sizeof(short);
            size += ifcBin.NumberListValues.Count * sizeof(short);
            size += ifcBin.SymbolListValues.Count * sizeof(short);
            size += ifcBin.EntityNameListValues.Count * sizeof(short);
            size += ifcBin.Lists.Count * sizeof(int);
            size += ifcBin.Vars.Count * sizeof(int);

            size += ifcBin.Vector2s.Count * sizeof(Vector2);
            size += ifcBin.Vector3s.Count * sizeof(Vector3);
            size += ifcBin.Vector4s.Count * sizeof(Vector4);

            Console.WriteLine($"Estimated size of IFC-Bin is {size:N0} bytes");
        }

        public static IfcBinaryGeometry ToBinaryGeometry(StepDocument doc)
        {
            var r = new IfcBinaryGeometry();
            var res = new StepValueResolver(doc);
            //var points = new List<StepValue>();
            //var loops = new List<StepValue>();
            //var faces = new List<StepValue>();
            //var bounds = new List<StepValue>();

            foreach (var pair in res.GetDefinitionIdsAndValues())
            {
                var id = pair.Item1;
                var val = pair.Item2;
                var name = val.GetEntityName();
                var attrs = val.GetEntityAttributesValue().GetElements().ToList();
                switch (name)
                {
                    case "IFCCARTESIANPOINT":
                    {
                        r.PointIds.Add(id);
                        var coords = attrs[0].AsNumberList();
                        r.PointXs.Add((float)coords[0]);
                        r.PointYs.Add(coords.Count > 1 ? (float)coords[1] : 0f);
                        r.PointZs.Add(coords.Count > 2 ? (float)coords[2] : 0f);
                        break;
                    }
                    case "IFCPOLYLOOP":
                    {
                        r.LoopIds.Add(id);
                        r.LoopPointOffset.Add(r.LoopPoints.Count);
                        var ids = attrs[0].AsIdList();
                        for (var i = 0; i < ids.Count; i++)
                        {
                            r.LoopPoints.Add(ids[i]);
                        }

                        break;
                    }
                    case "IFCFACE":
                    {
                        r.FaceIds.Add(id);
                        r.FaceLoopOffsets.Add(r.FaceLoops.Count);
                        var ids = attrs[0].AsIdList();
                        for (var i = 0; i < ids.Count; i++)
                        {
                            // TODO: these are actually bounds ids. 
                            r.FaceLoops.Add(ids[i]);
                        }

                        break;
                    }
                }
            }

            return r;
        }


        [Test]
        public static void Rewrite()
        {
            var logger = Logger.Console;
            using var doc = new StepDocument(InputFile, logger);
            logger.Log($"Loaded {doc.FilePath.GetFileName()}");
            Console.WriteLine($"Input file size = {InputFile.GetFileSize():N0}");
            var temp1 = InputFile.ChangeDirectoryAndExt(Path.GetTempPath(), "ifc.tmp1");
            temp1.Delete();
            var output = temp1.OpenWrite();
            for (var i = 0; i < doc.Definitions.Count; i++)
            {
                var def = doc.Definitions[i];
                var entityName = doc.RawValueData.GetEntityName(def);
                

                // Geometric data
                if (entityName == "IFCCARTESIANPOINT" || entityName == "IFCPOLYLOOP" || entityName == "IFCFACEOUTERBOUND" || entityName == "IFCFACE")
                    continue;

                // Property data 
                if (entityName == "IFCPROPERTYSINGLEVALUE" || entityName == "IFCRELDEFINESBYPROPERTIES" || entityName == "IFCPROPERTYSET")
                    continue; 

                output.Write(doc.BeforeDef(i));
                output.Write(def.AsSpan());
            }
            output.Write(doc.Epilogue());
            output.Flush();
            output.Close();
            Console.WriteLine($"Output file size = {temp1.GetFileSize():N0}");

            var bg = ToBinaryGeometry(doc);
            var size = 0;
            size += bg.PointIds.Count * 4;
            size += bg.PointXs.Count * 4;
            size += bg.PointYs.Count * 4;
            size += bg.PointZs.Count * 4;
            size += bg.LoopIds.Count * 4;
            size += bg.LoopPoints.Count * 4;
            size += bg.LoopPointOffset.Count * 4;
            size += bg.FaceIds.Count * 4;
            size += bg.FaceLoops.Count * 4;
            size += bg.FaceLoopOffsets.Count * 4;
            Console.WriteLine($"Binary geometry size = {size:N0}");

            var pd = new IfcPropData(doc);
            Console.WriteLine($"Binary property data size = {pd.SizeEstimate():N0}");
        }

        [Test]
        public static void TestGeometry()
        {
            var logger = Logger.Console;
            using var doc = new StepDocument(InputFile, logger);
            logger.Log($"Loaded {doc.FilePath.GetFileName()}");
            var cnt = 0;
            var points = new Dictionary<StepToken, Vector3>();
            var loops = new Dictionary<StepToken, StepToken[]>();
            var faces = new Dictionary<StepToken, StepToken[]>();
            var faceBounds = new Dictionary<StepToken, (StepToken, string)>();
            var boundingBoxes = new Dictionary<StepToken, (StepToken, double, double, double)>();
            var data = doc.RawValueData;
            
            //var defLookup = doc.Definitions.ToDictionary(def => def.Id, def => def);

            foreach (var def in doc.Definitions)
            {
                var defEntityVal = data.GetEntityValue(def);
                var defAttrVal = data.GetEntityAttributesValue(def);
                var attr = data.GetAttributes(def);
                var name = data.GetEntityName(def);
                if (name == "IFCCARTESIANPOINT")
                {
                    Debug.Assert(attr.Length > 1);
                    var point = data.AsPoint(attr[0]);
                    points.Add(def.IdToken, point);
                }

                if (name == "IFCPOLYLOOP")
                {
                    Debug.Assert(attr.Length > 1);
                    var ids = data.AsTokens(attr[0]);
                    loops.Add(def.IdToken, ids);
                }

                if (name == "IFCFACE")
                {
                    Debug.Assert(attr.Length > 1);
                    var ids = data.AsTokens(attr[0]);
                    faces.Add(def.IdToken, ids);
                }

                if (name == "IFCFACEBOUND" || name == "IFCFACEOUTERBOUND")
                {
                    Debug.Assert(attr.Length == 2);
                    var faceId = data.AsToken(attr[0]);
                    var symbol = data.AsTrimmedString(attr[1]);
                    Debug.Assert(symbol == "T" || symbol == "F", $"Unexpected symbol: {symbol}");
                    faceBounds.Add(def.IdToken, (faceId, symbol));
                }

                if (name == "IFCBOUNDINGBOX")
                {
                    Debug.Assert(attr.Length == 4);
                    var cornerId = data.AsToken(attr[0]);
                    var x = data.AsNumber(attr[1]);
                    var y = data.AsNumber(attr[2]);
                    var z = data.AsNumber(attr[3]);
                    boundingBoxes.Add(def.IdToken, (cornerId, x, y, z));
                }
            }
            logger.Log($"Found {points.Count} points");
            logger.Log($"Found {loops.Count} loops");
            logger.Log($"Found {faces.Count} faces");
            logger.Log($"Found {faceBounds.Count} face loops");
            logger.Log($"Found {boundingBoxes.Count} bounding boxes");

            var loopPoints = new Dictionary<StepToken, List<Vector3>>();
            foreach (var kv in loops)
            {
                var tmp =  new List<Vector3>();
                foreach (var id in kv.Value)
                {
                    var pt = points[id];
                    tmp.Add(pt);
                }
                loopPoints.Add(kv.Key, tmp);
            }

            logger.Log($"Completed loop points dictionary with  # {loopPoints.Count} entries");

            foreach (var kv in faces)
            {
                var boundsIds = kv.Value;
                Debug.Assert(boundsIds.Length >= 1);

                for (var i=0; i < boundsIds.Length; ++i)
                {
                    var boundsId = boundsIds[i];
                    var bounds = faceBounds[boundsId];
                    if (bounds.Item2 != "T")
                    {
                        throw new Exception("I expected the bounds direction to be 'T'");
                    }

                    var loopsId = bounds.Item1;
                    if (!loopPoints.TryGetValue(loopsId, out var loopPts))
                    {
                        throw new Exception($"Loop {loopsId} not found in loopPoints");
                    }
                }
            }

            logger.Log($"Validated {faces.Count} faces");
        }

        // Returns a random CSS hex like "#1f77b4" with high contrast vs white.
        // Set minContrast to 7.0 for AAA-level contrast, if you want it even darker.
        public static string RandomHighContrastHex(double minContrast = 4.5)
        {
            if (minContrast <= 1.0 || minContrast > 21.0)
                throw new ArgumentOutOfRangeException(nameof(minContrast), "Contrast must be in (1, 21].");

            // Precompute the luminance threshold needed to meet the contrast ratio vs white.
            // contrast = (Lwhite + 0.05) / (Lcolor + 0.05)  => Lcolor <= (1.05 / contrast) - 0.05
            double maxL = 1.05 / minContrast - 0.05;

            while (true)
            {
                byte r = (byte)Random.Shared.Next(256);
                byte g = (byte)Random.Shared.Next(256);
                byte b = (byte)Random.Shared.Next(256);

                if (RelativeLuminance(r, g, b) <= maxL)
                    return $"#{r:X2}{g:X2}{b:X2}".ToLowerInvariant();
            }
        }

        // WCAG relative luminance for sRGB
        private static double RelativeLuminance(byte R, byte G, byte B)
        {
            static double ToLinear(double c)
                => (c <= 0.04045) ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

            double r = ToLinear(R / 255.0);
            double g = ToLinear(G / 255.0);
            double b = ToLinear(B / 255.0);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}
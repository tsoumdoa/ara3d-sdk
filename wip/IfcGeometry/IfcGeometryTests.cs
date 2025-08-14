using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
using Ara3D.IO.StepParser;
using Ara3D.Logging;
using Ara3D.Utils;

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
            PathUtil.GetCallerSourceFolder().RelativeFile("..", "..", "data", "AC20-FZK-Haus.ifc");
        //@"C:\Users\cdigg\data\Ara3D\snapshot-data\ifc\long_running\B11ALL.ifc";

        public class StepRelations
        {
            public readonly string SrcName;
            public readonly UInt128 SrcId;
            public readonly UInt128 RefId;
            public StepRelations(string srcName, UInt128 srcId, UInt128 refId)
            {
                SrcName = srcName;
                SrcId = srcId;
                RefId = refId;
            }
        }

        public static void AddRelations(StepDocument doc, string srcName, UInt128 srcId, List<StepRelations> relations, StepValue val)
        {
            if (val.IsId)
            {
                var id = doc.ValueData.AsId(val);
                relations.Add(new(srcName, srcId, id));
            }
            else if (val.IsList)
            {
                var attrs = doc.ValueData.AsArray(val);
                foreach (var attr in attrs)
                    AddRelations(doc, srcName, srcId, relations, attr);
            }
        }
        
        public static List<StepRelations> GetRelations(StepDocument doc)
        {
            var r = new List<StepRelations>();
            foreach (var def in doc.Definitions)
            {
                var srcName = doc.ValueData.GetEntityName(def);
                var srcId = def.Id;
                var val = doc.ValueData.GetAttributesValue(def);
                AddRelations(doc, srcName, srcId, r, val);
            }
            return r;
        }

        [Test]
        public static void FindRelations()
        {
            var logger = Logger.Console;

            // var f = InputFile;
            var f = @"C:\Users\cdigg\data\Ara3D\impraria\0000100120-093 - OXAGON ADVANCED HEALTH CENTER\STAGE 3A - CONCEPT DESIGN\ARC\03-730000-0000100120-DAH-ARC-MDL-000009 _IFC_D.ifc";
            using var doc = new StepDocument(f, logger);

            logger.Log($"Loaded {doc.FilePath.GetFileName()}");
            var relations = GetRelations(doc);
            logger.Log($"Found {relations.Count} relations");
            var groups = relations.GroupBy(r => r.SrcName);
            foreach (var group in groups.OrderBy(g => g.Key))
            {
                logger.Log($"{group.Key} has {group.Count()} relations");
            }
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
                var names = doc.Definitions.Select(doc.ValueData.GetEntityName).Distinct().ToList();
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
                Console.WriteLine($"{def.IdToken}= {doc.ValueData.ToString(def)}");
            }
        }

        public static Vector3 AsPoint(this StepValueData data, StepValue val)
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
        public static void TestGeometry()
        {
            var logger = Logger.Console;
            using var doc = new StepDocument(InputFile, logger);
            logger.Log($"Loaded {doc.FilePath.GetFileName()}");
            var cnt = 0;
            var points = new Dictionary<UInt128, Vector3>();
            var loops = new Dictionary<UInt128, UInt128[]>();
            var faces = new Dictionary<UInt128, UInt128[]>();
            var faceBounds = new Dictionary<UInt128, (UInt128, string)>();
            var data = doc.ValueData;
            
            //var defLookup = doc.Definitions.ToDictionary(def => def.Id, def => def);

            foreach (var def in doc.Definitions)
            {
                var defEntityVal = data.GetEntityValue(def);
                var defAttrVal = data.GetAttributesValue(def);
                var attr = data.GetAttributes(def);
                var name = data.GetEntityName(def);
                if (name == "IFCCARTESIANPOINT")
                {
                    Debug.Assert(attr.Length > 1);
                    var point = data.AsPoint(attr[0]);
                    points.Add(def.Id, point);
                }

                if (name == "IFCPOLYLOOP")
                {
                    Debug.Assert(attr.Length > 1);
                    var ids = data.AsIds(attr[0]);
                    loops.Add(def.Id, ids);
                }

                if (name == "IFCFACE")
                {
                    Debug.Assert(attr.Length > 1);
                    var ids = data.AsIds(attr[0]);
                    faces.Add(def.Id, ids);
                }

                if (name == "IFCFACEBOUND" || name == "IFCFACEOUTERBOUND")
                {
                    Debug.Assert(attr.Length == 2);
                    var faceId = data.AsId(attr[0]);
                    var symbol = data.AsTrimmedString(attr[1]);
                    Debug.Assert(symbol == "T" || symbol == "F", $"Unexpected symbol: {symbol}");
                    faceBounds.Add(def.Id, (faceId, symbol));
                }
            }
            logger.Log($"Found {points.Count} points");
            logger.Log($"Found {loops.Count} loops");
            logger.Log($"Found {faces.Count} faces");
            logger.Log($"Found {faceBounds.Count} face loops");

            var loopPoints = new Dictionary<UInt128, List<Vector3>>();
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
    }
}
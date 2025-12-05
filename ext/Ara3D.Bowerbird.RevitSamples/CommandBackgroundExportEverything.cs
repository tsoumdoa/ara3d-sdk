using Ara3D.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Ara3D.BimOpenSchema;
using Ara3D.BimOpenSchema.IO;
using Parquet;
using Document = Autodesk.Revit.DB.Document;
using FilePath = Ara3D.Utils.FilePath;

namespace Ara3D.Bowerbird.RevitSamples;

public class CommandBackgroundExportEverything : NamedCommand
{
    public BosBackgroundExporterForm BosForm;
    public List<long> Ids = new();
    public int QueueSize;
    public override string Name => "BOM Background Export";
    public BimOpenSchemaRevitBuilder RevitBuilder;
    public UIApplication UiApp;
    public string Folder = @"C:\dev\aec-tech-linter\tmp\";
    public int LastCount = 0;
    public List<long> DeletedIds = new List<long>();
    public bool Modified = false;
    public DateTimeOffset LastSave = DateTimeOffset.Now;
    public const double UpdateFrequency = 0.5;

    public static Guid AddInGuid => CommandBackgroundExportRooms.AddInGuid;

    public override void Execute(object arg)
    {
        BosForm = new BosBackgroundExporterForm();
        BosForm.FormClosing += BosFormOnFormClosing;
        BosForm.Show();

        UiApp = arg as UIApplication;
        Doc = UiApp?.ActiveUIDocument?.Document;
        Ids.Clear();

        RevitBuilder = new(Doc, true, false);
        
        UiApp.Application.DocumentChanged += OnDocumentChanged;

        Processor = new BackgroundProcessor<long>(ProcessElementById, UiApp);
        Processor.ExecuteNextIdleImmediately = false;
        Processor.DoWorkDuringIdle = true;
        Processor.OnHeartbeat += ProcessorOnHeartbeat;
        Processor.OnIdle += ProcessorOnIdle;
        BosForm.OnReset += BosFormOnOnReset;
    }

    private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
    {
        Modified = true;
        var added = e.GetAddedElementIds();
        var deleted = e.GetDeletedElementIds();
        var modified = e.GetModifiedElementIds();

        var tmp = added.Select(e => e.Value).ToList();
        tmp.AddRange(deleted.Select(e => e.Value));
        tmp.AddRange(modified.Select(e => e.Value));
        Processor.EnqueueWork(tmp);
        BosForm?.SetQueueSize(QueueSize += tmp.Count);
    }

    private void BosFormOnFormClosing(object sender, FormClosingEventArgs e)
    {
        Processor.Dispose();
        BosForm = null;
        UiApp.Application.DocumentChanged -= OnDocumentChanged;
    }

    private void BosFormOnOnReset(object sender, EventArgs e)
    { }

    private void ProcessorOnHeartbeat(object sender, EventArgs e)
    {
        BosForm?.SetHeartBeat();
        SaveData();
    }

    private void ProcessorOnIdle(object sender, EventArgs e)
    {
        BosForm?.SetIdle();
        SaveData();
    }

    public void SaveData()
    {
        try
        {
            var elapsed = DateTimeOffset.Now - LastSave;
            if (elapsed.TotalSeconds < UpdateFrequency) return;

            var bimData = RevitBuilder.Builder.Data;
            var dataSet = bimData.ToDataSet();

            var fp = new FilePath($@"C:\dev\aec-tech-linter\tmp\changes-{PathUtil.GetTimeStamp()}.parquet.zip");
            var fs = new FileStream(fp, FileMode.Create, FileAccess.Write, FileShare.None);

            using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

            var parquetCompressionMethod = CompressionMethod.Brotli;
            var parquetCompressionLevel = CompressionLevel.Optimal;
            var zipCompressionLevel = CompressionLevel.Fastest;

            dataSet.WriteParquetToZip(zip,
                parquetCompressionMethod,
                parquetCompressionLevel,
                zipCompressionLevel);

            LastSave = DateTimeOffset.Now;
            Modified = false;
            RevitBuilder = new(Doc, true, false);
        }
        catch
        {
            // Swallow exception
        }
    }

    public void ProcessElementById(long id)
    {
        using var el = Doc.GetElement(new ElementId(id));
        if (el == null)
        {
            DeletedIds.Add(id);
        }
        else
        {
            RevitBuilder.ProcessElement(el);
        }
    }

    public Document Doc;
    public BackgroundProcessor<long> Processor;
}
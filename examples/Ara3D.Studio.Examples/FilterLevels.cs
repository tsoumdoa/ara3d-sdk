using Ara3D.BimOpenSchema;

namespace Ara3D.Studio.Samples;

public class FilterLevels : IModelModifier
{
    [Options(nameof(LevelNames))] 
    public int Level;

    public List<string> LevelNames { get; private set; }

    [ComputedRange(nameof(_numLevels))]
    public int LevelSlider
    {
        get => Level; 
        private set => Level = value;
    }

    private int _numLevels => LevelNames.Count;
    private BimModel3D _bim;
    private List<(string Name, float Elevation)> _levelData;

    public void RecomputeLevels(BimModel3D bim)
    {
        _bim = bim;
        _levelData = bim.ObjectModel.GetDistinctLevels().ToList();
        LevelNames = _levelData.Select(x => $"{x.Name} {x.Elevation:F2}").ToList();
    }

    public IModel3D Eval(IModel3D model3D, EvalContext context)
    {
        if (model3D is not BimModel3D bim)
        {
            _bim = null;
            LevelNames = null;
            return model3D;
        }

        if (_bim != bim)
        {
            RecomputeLevels(bim);
            context.Application.RefreshUI(this);
        }

        var curLevelName = _levelData[Level].Name;
        var curLevelElevation = _levelData[Level].Elevation;

        bool FilterLevel(InstanceStruct inst)
        {
            var em = bim.GetEntityModel(inst);
            return em.LevelName == curLevelName && (Math.Abs(em.Elevation - curLevelElevation) < 0.0001);
        }

        return model3D.Where(FilterLevel);
    }
}
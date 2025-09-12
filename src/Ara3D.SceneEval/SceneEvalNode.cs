using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Ara3D.PropKit;
using Ara3D.Utils;

namespace Ara3D.SceneEval;

/// <summary>
/// Wraps an object that has an `Eval` method and an optional `Name` property.
/// The number of inputs must match the number of parameters in the `Eval` method.
/// All public fields and properties on that object are exposed as an `IPropContainer` 
/// </summary>
public class SceneEvalNode : IDisposable, INotifyPropertyChanged
{

    public SceneEvalGraph Graph { get; }
    public SceneEvalNode Input { get; set; }
    public PropProviderWrapper PropProvider { get; private set; }
    public object EvaluatableObject { get; private set; }
    public Attribute[] EvaluatableObjectAttributes { get; private set; }
    public bool InvalidateOnPropChange { get; private set; } = true;
    public string Name { get; private set; }
    private object _cached;
    private object[] _args;
    private Func<object[], object> _evalFunc;
    public event PropertyChangedEventHandler PropertyChanged;
    private bool _enabled = true;

    public SceneEvalNode(SceneEvalGraph graph, object evaluableObject)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        UpdateEvaluatableObject(evaluableObject);
    }

    public override string ToString()
    {
        return $"{GetType().Name}:{Name}";
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;
            _enabled = value;
            InvalidateCache();
        }
    }


    private void PropProvider_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (InvalidateOnPropChange)
            InvalidateCache();
    }

    public object GetCachedObject()
        => _cached;

    public object Eval(EvalContext context)
    {
        if (_cached == null)
            Interlocked.CompareExchange(ref _cached, EvalCore(context), null);
        return _cached;
    }

    public void InputInvalidated(object sender, EventArgs e)
    {
        InvalidateCache();
    }

    public void SetInput(SceneEvalNode input)
    {
        Input = input;
        InvalidateCache();
        Graph.NotifyGraphChanged(this, EventArgs.Empty);
    }

    private object EvalCore(EvalContext context)
    {
        if (Input != null)
        {
            Debug.Assert(_args.Length >= 2);
            var inputVal = Input.Eval(context);
            _args[0] = inputVal;
            if (inputVal == null)
                return null;
        }

        _args[^1] = context;

        if (!Enabled)
        {
            return _args.Length >= 2 
                ? _args[0] 
                : null;
        }

        return _cached = _evalFunc(_args);
    }

    public bool IsCacheValid()
        => _cached != null;

    public void InvalidateCache(bool notify = true)
    {
        _cached = null;
        if (notify)
            Graph.UpdateDownstreamCaches(this);
    }

    public List<SceneEvalNode> GetInputPath()
    {
        var list = new List<SceneEvalNode>();
        var cur = this;
        while (cur != null)
        {
            list.Add(cur);
            cur = cur.Input; 
        }
        return list;
    }

    public SceneEvalNode GetSource()
    {
        var cur = this;
        while (!cur.IsSource)
            cur = cur.Input;
        return cur;
    }

    public bool IsSource
        => Input == null;

    public void Dispose()
        => PropProvider.Dispose();
    
    private string GetName(object obj)
    {
        var type = obj.GetType();
        var prop = type.GetProperty("Name");
        var name = prop?.GetValue(obj)?.ToString();
        return name ?? type.Name.SplitCamelCase();
    }

    private (object[], Func<object[], object>) GetArgsAndEvalFunction(object obj)
    {
        var type = obj.GetType();
        var func = type.GetMethod("Eval");
        if (func == null)
            throw new InvalidOperationException($"The object {obj} does not have an Eval method.");
        var args = new object[func.GetParameters().Length];
        return (args, (localArgs) => func.Invoke(EvaluatableObject, localArgs));
    }

    public void UpdateEvaluatableObject(object obj)
    {
        if (obj == null) throw new Exception("Evaluatable object cannot be null.");

        (_args, _evalFunc) = GetArgsAndEvalFunction(obj);
        Name = GetName(obj);
        var newWrapper = obj.GetBoundPropProvider();

        // Remove the old property provider, but first copy the values from it
        if (PropProvider != null)
        {
            var props = PropProvider.GetPropValues();
            foreach (var prop in props)
                if (!prop.Descriptor.IsReadOnly)
                    newWrapper.TrySetValue(prop.Descriptor, prop.Value);

            PropProvider.Dispose();
        }

        EvaluatableObject = obj;
        EvaluatableObjectAttributes = [];

        // Determine if invalidation happens automatically, or is explicit
        InvalidateOnPropChange = true; 
        var t = obj.GetType();
        EvaluatableObjectAttributes = t.GetCustomAttributes().ToArray();
        var applyModeAttr = EvaluatableObjectAttributes.OfType<ApplyModeAttribute>().FirstOrDefault();
        if (applyModeAttr != null)
        {
            InvalidateOnPropChange = applyModeAttr.Mode == ApplyMode.Dynamic;
        }

        PropProvider = newWrapper;
        PropProvider.PropertyChanged += PropProvider_PropertyChanged;
    }
}
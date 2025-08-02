using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ara3D.IO.StepParser;

/// <summary>
/// This is 64 bits long. The "kind" data is stored in the lower 4 bits of _count.
/// The _index is into either the value list or the token list of StepValueData
/// </summary>
public readonly struct StepValue
{
    private readonly uint _index;
    private readonly uint _count;

    /// <summary>
    /// Constructs a StepValue with the given kind, index, and count.
    /// The count must be less than (2^28) to fit into the upper 28 bits of _count.
    /// </summary>
    public StepValue(StepKind kind, int index, int count = 0)
    {
        _index = (uint)index;
        Debug.Assert(count < (uint.MaxValue >> 4));
        _count = (uint)count << 4 | (uint)kind;
    }

    /// <summary>
    /// Returns the kind of the value. 
    /// </summary>
    public StepKind Kind
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get  => (StepKind)(_count & 0xF);
    }
    
    /// <summary>
    /// Returns an index into either the value list or the token list. 
    /// </summary>
    public int Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)_index;
    }

    /// <summary>
    /// Returns the number of index into the value list: only use for StepKind.List
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_count >> 4);
    }

    /// <summary>
    /// Returns the number of index into the value list: only use for StepKind.List
    /// </summary>
    public override string ToString()
        => $"{Kind}[{Count}] + {Index}";

    //==
    // Various helpers 

    public bool IsEntity 
        => Kind == StepKind.Entity;
    
    public bool IsId 
        => Kind == StepKind.Id;
    
    public bool IsList 
        => Kind == StepKind.List;
    
    public bool IsUnassigned 
        => Kind == StepKind.Unassigned;
    
    public bool IsRedeclared 
        => Kind == StepKind.Redeclared;
    
    public bool IsString 
        => Kind == StepKind.String;
    
    public bool IsNumber 
        => Kind == StepKind.Number;
}
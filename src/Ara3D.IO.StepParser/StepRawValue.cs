using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ara3D.IO.StepParser;

/// <summary>
/// This is 64 bits long. The "kind" data is stored in the lower 4 bits of _count.
/// The _index is into either the value list or the token list of StepValueData
/// </summary>
public readonly struct StepRawValue
{
    /// <summary>
    /// This is an index into either the token index, or the value
    /// </summary>
    private readonly uint _index;

    /// <summary>
    /// When encoding lists, this is the number of elements in the list.
    /// When encoding entities, this is the index of the entity attributes (which are a list)
    /// </summary>
    private readonly uint _countOrOffset;

    /// <summary>
    /// Constructs a StepValue with the given kind, index, and count.
    /// The count must be less than (2^28) to fit into the upper 28 bits of _count.
    /// </summary>
    public StepRawValue(StepKind kind, int index, int countOrOffset = 0)
    {
        _index = (uint)index;
        Debug.Assert(countOrOffset < (uint.MaxValue >> 4));
        _countOrOffset = (uint)countOrOffset << 4 | (uint)kind;
    }

    /// <summary>
    /// Returns the kind of the value. 
    /// </summary>
    public StepKind Kind
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get  => (StepKind)(_countOrOffset & 0xF);
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
    /// Returns the number of entries in the value list is a StepKind.List,
    /// or if it is a StepKind.Entity then this is the index where the list
    /// entity attributes start. 
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_countOrOffset >> 4);
    }

    /// <summary>
    /// For entities, returns the value index of the entity attributes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEntityAttributeValueIndex()
    {
        Debug.Assert(IsEntity);
        return Count;
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

    public bool IsSymbol
        => Kind == StepKind.Symbol;
}
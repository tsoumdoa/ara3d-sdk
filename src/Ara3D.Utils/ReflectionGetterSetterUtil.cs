using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ara3D.Utils;

// A generic typed function for getting values.
public delegate TValue Getter<TTarget, TValue>(TTarget target);

// A generic typed function for setting values. Notice that it requires a "ref". 
public delegate void Setter<TTarget, TValue>(ref TTarget target, TValue value);

/// <summary>
/// This class generates delegates to quickly get or set values.
/// In theory this should be faster than using Reflection methods. 
/// </summary>
public static class ReflectionGetterSetterUtil
{
    public static Setter<TTarget, TValue> GetFastSetter<TTarget, TValue>(this MemberInfo member)
        => (Setter<TTarget, TValue>)member.GetFastSetter();

    public static Getter<TTarget, TValue> GetFastGetter<TTarget, TValue>(this MemberInfo member)
        => (Getter<TTarget, TValue>)member.GetFastGetter();

    public static Delegate GetFastGetter(this MemberInfo member)
    {
        var tTarget = member.DeclaringType;
        var tValue = member.GetMemberType();

        ValidateMember(member);

        var target = Expression.Parameter(tTarget, "target");
        var instance = IsStatic(member) ? null! : (Expression)target;

        Expression body = member switch
        {
            PropertyInfo pi => Expression.Call(IsStatic(pi) ? null : instance, GetGetter(pi, false)),
            FieldInfo fi => fi.IsStatic ? Expression.Field(null, fi) : Expression.Field(instance, fi),
            _ => throw new NotSupportedException()
        };

        var delType = typeof(Getter<,>).MakeGenericType(tTarget, tValue);
        return Expression.Lambda(delType, body, target).Compile();
    }

    public static Delegate GetFastSetter(this MemberInfo member)
    {
        var tTarget = member.DeclaringType;
        var tValue = member.GetMemberType();

        ValidateMember(member);

        var targetByRef = Expression.Parameter(tTarget.MakeByRefType(), "target");
        var value = Expression.Parameter(tValue, "value");

        var instance = IsStatic(member) ? null! :
            (tTarget.IsValueType ? (Expression)targetByRef : Expression.Convert(targetByRef, tTarget));

        Expression body = member switch
        {
            PropertyInfo pi => Expression.Call(IsStatic(pi) ? null : instance, GetSetter(pi, false),
                EnsureType(value, pi.PropertyType)),
            FieldInfo fi => Expression.Assign(
                fi.IsStatic ? Expression.Field(null, fi) : Expression.Field(instance, fi),
                EnsureType(value, fi.FieldType)),
            _ => throw new NotSupportedException()
        };

        var delType = typeof(Setter<,>).MakeGenericType(tTarget, tValue);
        return Expression.Lambda(delType, body, targetByRef, value).Compile();
    }

    public static MethodInfo GetGetter(PropertyInfo pi, bool nonPublic)
        => pi.GetGetMethod(nonPublic) ?? throw new InvalidOperationException($"No getter for {pi.Name}");

    public static MethodInfo GetSetter(PropertyInfo pi, bool nonPublic)
        => pi.GetSetMethod(nonPublic) ?? throw new InvalidOperationException($"No setter for {pi.Name}");

    static bool IsStatic(PropertyInfo pi)
        => (pi.GetMethod ?? pi.SetMethod ?? throw new InvalidOperationException("No accessor")).IsStatic;

    static void ValidateMember(MemberInfo member)
    {
        if (member is PropertyInfo pi)
        {
            if (pi.GetIndexParameters().Length != 0)
                throw new NotSupportedException("Indexed properties are not supported.");
        }
        else if (member is FieldInfo)
        {
            // ok
        }
        else
        {
            throw new NotSupportedException("Member must be a PropertyInfo or FieldInfo.");
        }
    }

    static bool IsStatic(MemberInfo m) => m switch
    {
        PropertyInfo pi => ((pi.GetMethod ?? pi.SetMethod) ?? throw new InvalidOperationException("Property has no accessor.")).IsStatic,
        FieldInfo fi => fi.IsStatic,
        _ => false
    };

    static Type GetMemberType(this MemberInfo m) => m switch
    {
        PropertyInfo pi => pi.PropertyType,
        FieldInfo fi => fi.FieldType,
        _ => throw new NotSupportedException()
    };


    static Expression EnsureType(Expression value, Type targetType)
        => value.Type == targetType ? value : Expression.Convert(value, targetType);

    //=
    // TODO: Delete the following functions

    static Expression BuildPropertyGet(PropertyInfo pi, Expression? instance, bool nonPublic)
    {
        var getter = pi.GetGetMethod(nonPublic) ?? throw new InvalidOperationException($"No getter for {pi.Name}");
        return Expression.Call(pi.GetMethod!.IsStatic ? null : instance, getter);
    }

    static Expression BuildPropertySet(PropertyInfo pi, Expression? instance, Expression value, bool nonPublic)
    {
        var setter = pi.GetSetMethod(nonPublic) ?? throw new InvalidOperationException($"No setter for {pi.Name}");
        return Expression.Call(pi.SetMethod!.IsStatic ? null : instance, setter, EnsureType(value, pi.PropertyType));
    }

    static Expression BuildFieldGet(FieldInfo fi, Expression? instance)
        => fi.IsStatic ? Expression.Field(null, fi) : Expression.Field(instance!, fi);

    static Expression BuildFieldSet(FieldInfo fi, Expression? instance, Expression value)
        => fi.IsStatic
            ? Expression.Assign(Expression.Field(null, fi), EnsureType(value, fi.FieldType))
            : Expression.Assign(Expression.Field(instance!, fi), EnsureType(value, fi.FieldType));

    static Expression CastObjectTo(Expression valueObj, Type targetType)
    {
        if (valueObj.Type != typeof(object))
            throw new ArgumentException("valueObj must be of type object", nameof(valueObj));

        // Reference type: just cast (null stays null)
        if (!targetType.IsValueType)
            return Expression.Convert(valueObj, targetType);

        // Value type (including Nullable<T>): null -> default(T)
        var isNull = Expression.Equal(valueObj, Expression.Constant(null, typeof(object)));
        var whenNull = Expression.Default(targetType);

        // Nullable<T>: allow boxed T or boxed Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying is not null)
        {
            // If the incoming object is boxed underlying (e.g., int), unbox to underlying then lift to Nullable<T>
            var unboxUnderlying = Expression.Unbox(valueObj, underlying);          // (int)valueObj
            var liftToNullable = Expression.Convert(unboxUnderlying, targetType);  // (int?)((int)valueObj)
            return Expression.Condition(isNull, whenNull, liftToNullable);
        }

        // Non-nullable value type: require boxed exact type (or something directly unboxable)
        var unbox = Expression.Unbox(valueObj, targetType);
        return Expression.Condition(isNull, whenNull, unbox);
    }
}
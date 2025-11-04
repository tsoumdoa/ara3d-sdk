using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ara3D.Utils;

/// <summary>
/// This class generates delegates to quickly get or set values.
/// In theory this should be faster than using Reflection methods. 
/// </summary>
public static class ReflectionGetterSetterUtil
{
    public delegate void SetterRef<TTarget, TValue>(ref TTarget target, TValue value);

    public static Func<TTarget, TValue> GetFastTypedGetter<TTarget, TValue>(this MemberInfo member, bool nonPublic = false)
        => (Func<TTarget, TValue>)BuildTypedGetter(typeof(TTarget), typeof(TValue), member, nonPublic);

    public static Action<TTarget, TValue> GetFastTypedSetter<TTarget, TValue>(this MemberInfo member, bool nonPublic = false)
        => (Action<TTarget, TValue>)BuildTypedSetter(typeof(TTarget), typeof(TValue), member, nonPublic);

    // For struct instance fields/properties: allows true in-place mutation
    public static SetterRef<TTarget, TValue> GetFastTypedSetterRef<TTarget, TValue>(this MemberInfo member, bool nonPublic = false)
        => (SetterRef<TTarget, TValue>)BuildRefSetter(typeof(TTarget), typeof(TValue), member, nonPublic);

    public static Func<object, object> GetFastGetter(this MemberInfo member, bool nonPublic = false)
    {
        ValidateMember(member);

        var declaring = GetDeclaringType(member);
        var isStatic = IsStatic(member);

        var objParam = Expression.Parameter(typeof(object), "obj");

        Expression instanceExpr;
        if (isStatic)
        {
            instanceExpr = null!;
        }
        else
        {
            // obj == null ? throw : (DeclaringType)obj
            var cast = declaring.IsValueType
                ? (Expression)Expression.Unbox(objParam, declaring)
                : Expression.Convert(objParam, declaring);

            // Guard null
            var guard = Expression.IfThen(
                Expression.Equal(objParam, Expression.Constant(null, typeof(object))),
                Expression.Throw(Expression.New(typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) })!,
                                                Expression.Constant("obj")))
            );

            instanceExpr = Expression.Block(guard, cast);
        }

        Expression readExpr = member switch
        {
            PropertyInfo pi => BuildPropertyGet(pi, instanceExpr, nonPublic),
            FieldInfo fi => BuildFieldGet(fi, instanceExpr),
            _ => throw new NotSupportedException()
        };

        // box to object if needed
        var boxed = readExpr.Type.IsValueType ? Expression.Convert(readExpr, typeof(object)) : (Expression)readExpr;

        var lambda = Expression.Lambda<Func<object, object>>(boxed, objParam);
        return lambda.Compile();
    }

    public static Action<object, object> GetFastSetter(this MemberInfo member, bool nonPublic = false)
    {
        ValidateMember(member);

        var declaring = GetDeclaringType(member);
        var memberType = GetMemberType(member);
        var isStatic = IsStatic(member);

        var objParam = Expression.Parameter(typeof(object), "obj");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // You cannot mutate a boxed struct via object.
        if (!isStatic && declaring.IsValueType)
        {
            throw new NotSupportedException("Setting instance members on value types via object is not supported. Use CreateSetterRef<TTarget, TValue> and pass target by ref.");
        }

        Expression instanceExpr = isStatic ? null! :
            (declaring.IsValueType
                ? Expression.Unbox(objParam, declaring) // (but we disallowed this path above)
                : Expression.Convert(objParam, declaring));

        var valueCast = CastObjectTo(valueParam, memberType);

        Expression assignExpr = member switch
        {
            PropertyInfo pi => BuildPropertySet(pi, instanceExpr, valueCast, nonPublic),
            FieldInfo fi => BuildFieldSet(fi, instanceExpr, valueCast),
            _ => throw new NotSupportedException()
        };

        var lambda = Expression.Lambda<Action<object, object>>(assignExpr, objParam, valueParam);
        return lambda.Compile();
    }

    // -------------------- Builders (typed) --------------------

    static Delegate BuildTypedGetter(Type tTarget, Type tValue, MemberInfo member, bool nonPublic)
    {
        ValidateMember(member);

        if (GetDeclaringType(member) != tTarget)
            throw new ArgumentException("Member declaring type does not match TTarget.");

        if (GetMemberType(member) != tValue)
            throw new ArgumentException("Member type does not match TValue.");

        var targetParam = Expression.Parameter(tTarget, "target");

        Expression instanceExpr = IsStatic(member) ? null! : (Expression)targetParam;

        Expression readExpr = member switch
        {
            PropertyInfo pi => BuildPropertyGet(pi, instanceExpr, nonPublic),
            FieldInfo fi => BuildFieldGet(fi, instanceExpr),
            _ => throw new NotSupportedException()
        };

        var lambdaType = typeof(Func<,>).MakeGenericType(tTarget, tValue);
        return Expression.Lambda(lambdaType, readExpr, targetParam).Compile();
    }

    static Delegate BuildTypedSetter(Type tTarget, Type tValue, MemberInfo member, bool nonPublic)
    {
        ValidateMember(member);

        if (GetDeclaringType(member) != tTarget)
            throw new ArgumentException("Member declaring type does not match TTarget.");

        if (GetMemberType(member) != tValue)
            throw new ArgumentException("Member type does not match TValue.");

        var targetParam = Expression.Parameter(tTarget, "target");
        var valueParam = Expression.Parameter(tValue, "value");

        if (!IsStatic(member) && tTarget.IsValueType)
        {
            throw new NotSupportedException("Use CreateSetterRef<TTarget,TValue> for value-type instance setters.");
        }

        Expression instanceExpr = IsStatic(member) ? null! : (Expression)targetParam;

        Expression assignExpr = member switch
        {
            PropertyInfo pi => BuildPropertySet(pi, instanceExpr, valueParam, nonPublic),
            FieldInfo fi => BuildFieldSet(fi, instanceExpr, valueParam),
            _ => throw new NotSupportedException()
        };

        var lambdaType = typeof(Action<,>).MakeGenericType(tTarget, tValue);
        return Expression.Lambda(lambdaType, assignExpr, targetParam, valueParam).Compile();
    }

    // True ref setter for structs
    static Delegate BuildRefSetter(Type tTarget, Type tValue, MemberInfo member, bool nonPublic)
    {
        ValidateMember(member);

        if (GetDeclaringType(member) != tTarget)
            throw new ArgumentException("Member declaring type does not match TTarget.");

        if (GetMemberType(member) != tValue)
            throw new ArgumentException("Member type does not match TValue.");

        var byRefTarget = Expression.Parameter(tTarget.MakeByRefType(), "target");
        var valueParam = Expression.Parameter(tValue, "value");

        Expression instanceExpr = IsStatic(member) ? null! : (Expression)byRefTarget;

        Expression assignExpr = member switch
        {
            PropertyInfo pi => BuildPropertySet(pi, instanceExpr, valueParam, nonPublic),
            FieldInfo fi => BuildFieldSet(fi, instanceExpr, valueParam),
            _ => throw new NotSupportedException()
        };

        var delType = typeof(SetterRef<,>).MakeGenericType(tTarget, tValue);
        return Expression.Lambda(delType, assignExpr, byRefTarget, valueParam).Compile();
    }

    // -------------------- Helpers --------------------

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

    static Type GetDeclaringType(MemberInfo m) 
        => m.DeclaringType ?? throw new ArgumentException("No declaring type.");
    
    static bool IsStatic(MemberInfo m) => m switch
    {
        PropertyInfo pi => ((pi.GetMethod ?? pi.SetMethod) ?? throw new InvalidOperationException("Property has no accessor.")).IsStatic,
        FieldInfo fi => fi.IsStatic,
        _ => false
    };

    static Type GetMemberType(MemberInfo m) => m switch
    {
        PropertyInfo pi => pi.PropertyType,
        FieldInfo fi => fi.FieldType,
        _ => throw new NotSupportedException()
    };

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

    static Expression EnsureType(Expression value, Type targetType)
        => value.Type == targetType ? value : Expression.Convert(value, targetType);

    static Expression CastObjectTo(ParameterExpression valueObj, Type targetType)
    {
        // null → default(T) for value types, or null ref
        if (targetType.IsValueType)
        {
            _ = Expression.Convert(
                Expression.Condition(
                    Expression.Equal(valueObj, Expression.Constant(null)),
                    Expression.Default(typeof(object)),
                    valueObj),
                typeof(object));

            // Use Convert with nullable handling
            return Expression.Condition(
                Expression.Equal(valueObj, Expression.Constant(null)),
                Expression.Default(targetType),
                Expression.Convert(valueObj, targetType)
            );
        }

        return Expression.Convert(valueObj, targetType);
    }
}
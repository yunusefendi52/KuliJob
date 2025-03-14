using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace KuliJob.Internals;

internal record class MethodExprCall
{
    public required string DeclType { get; set; }

    public required string MethodName { get; set; }

    public IEnumerable<MethodExprCallArg?>? Arguments { get; set; }

    internal class MethodExprCallArg
    {
        public string TypeName { get; set; } = null!;

        // No direct object conversion in System.Text.Json
        public object? Value { get; set; }
    }
}

internal class ExpressionSerializer
{
    internal Serializer serializer = new();

    public string FromExpr<T>(Expression<Func<T, Task>> expr)
    {
        return FromExpr(expression: expr);
    }

    public string FromExpr(LambdaExpression expression)
    {
        var methodCall = FromExprToObject(expression);
        return serializer.Serialize(methodCall);
    }

    public MethodExprCall FromExprToObject(LambdaExpression expression)
    {
        if (expression.Body is not MethodCallExpression methodCallExpression)
        {
            throw new ArgumentException($"Expression method is not valid {expression.Body.Type}");
        }

        var declType = methodCallExpression.Method.DeclaringType?.AssemblyQualifiedName;
        if (declType is null)
        {
            throw new ArgumentException($"DeclaringType is not supported {declType}");
        }
        var methodName = methodCallExpression.Method.Name;
        var arguments = methodCallExpression.Arguments.Select(v =>
        {
            if (v is ConstantExpression constantExpression)
            {
                return new MethodExprCall.MethodExprCallArg
                {
                    TypeName = constantExpression.Type.AssemblyQualifiedName!,
                    Value = constantExpression.Value,
                };
            }
            else if (v is Expression expression)
            {
                var lambda = Expression.Lambda(expression);
                var value = lambda.Compile().DynamicInvoke();
                return new MethodExprCall.MethodExprCallArg
                {
                    TypeName = expression.Type.AssemblyQualifiedName!,
                    Value = value,
                };
            }
            throw new ArgumentException($"Argument is not supported {v.Type}");
        });

        return new()
        {
            DeclType = declType!,
            MethodName = methodName,
            Arguments = arguments,
        };
    }

    public Task InvokeExpr(IServiceProvider serviceProvider, string expr)
    {
        var methodExprCall = serializer.Deserialize<MethodExprCall>(expr)!;
        return InvokeExpr(serviceProvider, methodExprCall);
    }

    public async Task InvokeExpr(IServiceProvider serviceProvider, MethodExprCall methodExprCall)
    {
        var declType = Type.GetType(methodExprCall.DeclType, false) ?? throw new ArgumentException($"Type not found {methodExprCall.DeclType}");
        var instance = declType.IsAbstract && declType.IsSealed ? null : ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, declType);
        var arguments = methodExprCall.Arguments?.Select(v => HandleElementTypes(v!)).ToArray();
        var argTypes = methodExprCall.Arguments?.Select(v => Type.GetType(v!.TypeName))?.ToArray() ?? [];
        var methodInfo = declType.GetMethod(methodExprCall.MethodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, argTypes!)
             ?? throw new ArgumentException($"Method not found {methodExprCall.MethodName} in {methodExprCall.DeclType}");
        var invoke = methodInfo.Invoke(instance, arguments)!;
        if (methodInfo.ReturnType == typeof(Task))
        {
            var invokeTask = (Task)invoke;
            await invokeTask;
        }
    }

    internal static object? HandleElementTypes(MethodExprCall.MethodExprCallArg exprCallArg)
    {
        var jsonElNull = exprCallArg.Value as JsonElement?;
        if (!jsonElNull.HasValue)
        {
            return null;
        }
        var type = Type.GetType(exprCallArg.TypeName);
        var jsonEl = jsonElNull.Value;
        if (type == typeof(string))
        {
            return jsonEl.GetString();
        }
        else if (type == typeof(int))
        {
            return jsonEl.TryGetInt32(out var value) ? value : null;
        }
        else if (type == typeof(short))
        {
            return jsonEl.TryGetInt16(out var value) ? value : null;
        }
        else if (type == typeof(long))
        {
            return jsonEl.TryGetInt64(out var value) ? value : null;
        }
        else if (type == typeof(Guid))
        {
            return jsonEl.TryGetGuid(out var value) ? value : null;
        }
        else if (type == typeof(DateTime))
        {
            return jsonEl.TryGetDateTime(out var value) ? value : null;
        }
        else if (type == typeof(DateTimeOffset))
        {
            return jsonEl.TryGetDateTimeOffset(out var value) ? value : null;
        }
        else if (type == typeof(bool))
        {
            return jsonEl.GetBoolean();
        }
        else if (type == typeof(decimal))
        {
            return jsonEl.TryGetDecimal(out var value) ? value : null;
        }

        throw new ArgumentException($"Argument type not supported {type}");
    }
}
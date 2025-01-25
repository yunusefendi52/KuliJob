using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace KuliJob.Internals;

internal class MethodExprCall
{
    public required string DeclType { get; set; }

    public required string MethodName { get; set; }

    public IEnumerable<MethodExprCallArg?>? Arguments { get; set; }
}

internal class MethodExprCallArg
{
    public string TypeName { get; set; } = null!;
    // No direct object conversion in System.Text.Json
    public object? Value { get; set; }
}

internal class ExpressionSerializer(IServiceProvider serviceProvider)
{
    internal Serializer serializer = new();

    public string SerializeExpr(Expression<Action> expression)
    {
        return SerializeLambdaExpr(expression);
    }

    public string SerializeExpr(Expression<Func<Task>> expression)
    {
        return SerializeLambdaExpr(expression);
    }

    public string SerializeExpr<T>(Expression<Func<T, Task>> expression)
    {
        return SerializeLambdaExpr(expression);
    }

    string SerializeLambdaExpr(LambdaExpression expression)
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
                return new MethodExprCallArg
                {
                    TypeName = constantExpression.Type.AssemblyQualifiedName!,
                    Value = constantExpression.Value,
                };
            }
            else if (v is Expression expression)
            {
                var lambda = Expression.Lambda(expression);
                var value = lambda.Compile().DynamicInvoke();
                return new MethodExprCallArg
                {
                    TypeName = expression.Type.AssemblyQualifiedName!,
                    Value = value,
                };
            }
            throw new ArgumentException($"Argument is not supported {v.Type}");
        });

        return serializer.Serialize<MethodExprCall>(new()
        {
            DeclType = declType!,
            MethodName = methodName,
            Arguments = arguments,
        });
    }

    public async Task InvokeExpr(string exprJson)
    {
        var methodExprCall = serializer.Deserialize<MethodExprCall>(exprJson) ?? throw new ArgumentException($"Expr not valid {exprJson}");
        var declType = Type.GetType(methodExprCall.DeclType, true) ?? throw new ArgumentException($"Type not found {methodExprCall.DeclType}");
        var instance = declType.IsAbstract && declType.IsSealed ? null : ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, declType);
        var arguments = methodExprCall.Arguments?.Select(v => HandleElementTypes(v!)).ToArray();
        var argTypes = methodExprCall.Arguments?.Select(v => Type.GetType(v!.TypeName))?.ToArray() ?? [];
        var methodInfo = declType.GetMethod(methodExprCall.MethodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, argTypes!)
             ?? throw new ArgumentException($"Method not found {methodExprCall.MethodName} in {methodExprCall.DeclType}");
        var invoke = methodInfo.Invoke(instance, arguments)!;
        if (methodInfo.ReturnType == typeof(Task))
        {
            var invokeTask = (Task)invoke;
            await invokeTask;
        }
    }

    internal static object? HandleElementTypes(MethodExprCallArg exprCallArg)
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
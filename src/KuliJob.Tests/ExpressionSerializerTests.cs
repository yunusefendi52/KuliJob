using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KuliJob.Internals;

namespace KuliJob.Tests;

public class ExpressionSerializerTests
{
    [Test]
    public async Task CanSerializeStaticTaskMethod()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var serializer = new ExpressionSerializer(sp);
        var lson = serializer.SerializeExpr(() => TaskMethod());
        await Verify(lson);
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsNothing();
    }

    static async Task TaskMethod()
    {
        await Console.Out.WriteLineAsync("TaskMethod");
    }

    [Test]
    public async Task CanSerializeTaskMethod_WithParams()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var serializer = new ExpressionSerializer(sp);
        var tmp = Path.GetTempFileName();
        var value = 50_000;
        var boolValue = true;
        string? nullValue = null;
        var guidValue = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;
        var dateTimeOffset = DateTimeOffset.UtcNow;
        var decimalValue = decimal.MinValue;
        var lson = serializer.SerializeExpr(() => TaskMethodParams(tmp, value, boolValue, nullValue, guidValue, dateTime, dateTimeOffset, decimalValue, int.MinValue, short.MaxValue, long.MaxValue));
        await serializer.InvokeExpr(lson);
        var equalValue = $"Test value {value} {boolValue} nullString {guidValue} {dateTime} {dateTimeOffset} {decimalValue} {int.MinValue} {short.MaxValue} {long.MaxValue}";
        await Assert.That(() => File.ReadAllTextAsync(tmp)).IsEqualTo(equalValue);
    }

    [Test]
    public async Task CanSerializeTaskMethod_WithInvalid_Tuple()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var serializer = new ExpressionSerializer(sp);
        var tuple = (0.5, double.MaxValue);
        var lson = serializer.SerializeExpr(() => TestMethodInvalidParams(tuple));
        await Assert.That(() => serializer.InvokeExpr(lson)).Throws<ArgumentException>();
    }

    static async Task TaskMethodParams(
        string filepath,
        int value,
        bool boolValue,
        string? nullValue,
        Guid guidValue,
        DateTime dateTime,
        DateTimeOffset dateTimeOffset,
        decimal decimalValue,
        int constValue,
        short shortValue,
        long longValue)
    {
        await File.WriteAllTextAsync(filepath, $"Test value {value} {boolValue} {(nullValue is null ? "nullString" : "")} {guidValue} {dateTime} {dateTimeOffset} {decimalValue} {constValue} {shortValue} {longValue}");
    }

    static async Task TestMethodInvalidParams((double, double) tuple)
    {
        await Console.Out.WriteLineAsync($"{tuple}");
    }

    [Test]
    public async Task Can_Resolve_Service_WithoutDI()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var serializer = new ExpressionSerializer(scope.ServiceProvider);
        var lson = serializer.SerializeExpr<MyService>(t => t.MyMethod());
        await Verify(lson);
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsNothing();
    }

    [Test]
    public async Task Can_Resolve_Interface_Service()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMyService, MyService>();
        var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var serializer = new ExpressionSerializer(scope.ServiceProvider);
        var lson = serializer.SerializeExpr<IMyService>(t => t.MyMethod());
        await Verify(lson);
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsNothing();

        lson = serializer.SerializeExpr<IMyService>(t => t.MyMethodArgs("test"));
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsNothing();
    }

    [Test]
    public async Task Should_Not_Resolve_Not_Registered_Interface_Service()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var serializer = new ExpressionSerializer(scope.ServiceProvider);
        var lson = serializer.SerializeExpr<IMyService>(t => t.MyMethod());
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsException();

        lson = serializer.SerializeExpr<IMyService>(t => t.MyMethodArgs("test"));
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsException();
    }

    [Test]
    public async Task Can_Execute_Action()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        await using var scope = sp.CreateAsyncScope();
        var serializer = new ExpressionSerializer(scope.ServiceProvider);
        var lson = serializer.SerializeExpr(() => Console.WriteLine("KuliJob WriteLine Test"));
        var writer = new StringWriter();
        Console.SetOut(writer);
        await Assert.That(() => serializer.InvokeExpr(lson)).ThrowsNothing();
        var reader = new StringReader(writer.ToString());
        var stdoutStr = await reader.ReadToEndAsync();
        await Assert.That(stdoutStr).IsEqualTo("KuliJob WriteLine Test\n");
    }
}

interface IMyService
{
    Task MyMethod();
    Task MyMethodArgs(string value);
}

internal class MyService : IMyService
{
    public async Task MyMethod()
    {
    }

    public async Task MyMethodArgs(string value)
    {
    }
}

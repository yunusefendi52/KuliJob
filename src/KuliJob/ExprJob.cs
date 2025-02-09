
namespace KuliJob;

internal class ExprJob : IJob
{
    public async Task Execute(JobContext context)
    {
        var type = context.JobData.GetValue<string>("k_type");
        var methodName = context.JobData.GetValue<string>("k_methodName");
        var args = context.JobData.GetValue<IEnumerable<MethodExprCall.MethodExprCallArg?>>("k_args");
        var exprSerializer = context.Services.GetRequiredService<ExpressionSerializer>();
        var serviceProvider = context.Services.GetRequiredService<IServiceProvider>();
        await exprSerializer.InvokeExpr(serviceProvider, new MethodExprCall
        {
            DeclType = type!,
            MethodName = methodName!,
            Arguments = args,
        });
    }
}

namespace KuliJob.CronJob;

internal class CronJobHandler(IServiceProvider serviceProvider, ExpressionSerializer expressionSerializer)
{
    public async Task Execute(MethodExprCall methodExprCall)
    {
        await expressionSerializer.InvokeExpr(serviceProvider, methodExprCall);
    }
}

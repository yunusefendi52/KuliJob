using System.Runtime.CompilerServices;
using DiffEngine;

namespace KuliJob.Tests;

public static class ModuleInitializer
{
    public static KTestType K_TestStorage = Enum.Parse<KTestType>(Environment.GetEnvironmentVariable("K_TEST_STORAGE", EnvironmentVariableTarget.Process) ?? KTestType.Sqlite.ToString(), true);

    [ModuleInitializer]
    public static void Init()
    {
        Verifier.UseProjectRelativeDirectory("verified_txts");
        DiffTools.UseOrder(DiffTool.VisualStudioCode);
    }
}


public enum KTestType
{
    Pg,
    Sqlite,
}

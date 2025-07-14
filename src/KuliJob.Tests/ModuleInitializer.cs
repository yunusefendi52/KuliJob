using System.Runtime.CompilerServices;
using DiffEngine;

namespace KuliJob.Tests;

public static class ModuleInitializer
{
    public static KTestType K_TestStorage = KTestType.Pg;

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

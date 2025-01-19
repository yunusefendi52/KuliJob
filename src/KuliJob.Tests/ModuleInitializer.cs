using System.Runtime.CompilerServices;
using DiffEngine;

namespace KuliJob.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        Verifier.UseProjectRelativeDirectory("verified_txts");
        DiffTools.UseOrder(DiffTool.VisualStudioCode);
    }
}

using System.Reflection;

namespace SupportToolsServerApplication;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
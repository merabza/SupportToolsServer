using Xunit;

namespace SupportToolsServer.Tests.TestInfrastructure;

//The real host sets the static Serilog logger when it starts and closes it when it stops (Program.cs and
//SupportToolsServerHostFactory). A test that reads the log of its host therefore runs alone, after the tests that run
//in parallel
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerialHostCollection
{
    public const string Name = "Serial host";
}

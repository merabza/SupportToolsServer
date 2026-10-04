using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Collects the console output until it is disposed. The host writes from its request threads while the test reads, so
//the text is locked. Console.Out is global: only a test that runs alone may use it (SerialHostCollection)
internal sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _original = Console.Out;
    private readonly LockedWriter _writer = new();

    public ConsoleCapture()
    {
        Console.SetOut(_writer);
    }

    public string Text => _writer.ToString();

    public void Dispose()
    {
        Console.SetOut(_original);
        _writer.Dispose();
    }

    //TextWriter writes every text through Write(char)
    private sealed class LockedWriter : TextWriter
    {
        private readonly Lock _lock = new();
        private readonly StringBuilder _text = new();

        public override Encoding Encoding => Encoding.Unicode;

        public override void Write(char value)
        {
            lock (_lock)
            {
                _text.Append(value);
            }
        }

        public override string ToString()
        {
            lock (_lock)
            {
                return _text.ToString();
            }
        }
    }
}

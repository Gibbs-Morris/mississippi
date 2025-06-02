using System;
using System.IO;
using Xunit;

namespace Mississippi.CrescentConsoleApp.Tests;

public class ProgramTests
{
    [Fact]
    public void Main_PrintsHelloWorld()
    {
        using StringWriter sw = new();
        TextWriter original = Console.Out;
        Console.SetOut(sw);
        global::Program.Main(Array.Empty<string>());
        Console.SetOut(original);
        Assert.Equal("Hello, World!" + Environment.NewLine, sw.ToString());
    }
}

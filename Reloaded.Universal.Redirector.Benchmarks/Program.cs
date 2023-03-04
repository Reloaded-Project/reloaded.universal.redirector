// See https://aka.ms/new-console-template for more information

using System.Reflection;
using BenchmarkDotNet.Running;
using Reloaded.Universal.Redirector.Benchmarks;

var benchmarks = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(x => x.IsAssignableTo(typeof(IBenchmark)) && x != typeof(IBenchmark))
    .ToArray();

while (true)
{
    Console.WriteLine("Select a Benchmark");
    
    var origColour = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Green;
    for (int x = 0; x < benchmarks.Length; x++)
        Console.WriteLine($"{x}. {benchmarks[x].Name}");

    Console.ForegroundColor = origColour;
    Console.WriteLine("Enter any invalid number to exit.");

    var line = Console.ReadLine();
    if (int.TryParse(line, out int result))
    {
        if (result < benchmarks.Length)
            BenchmarkRunner.Run(benchmarks[result]);
        else
            return;
    }
}
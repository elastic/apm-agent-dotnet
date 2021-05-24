using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteEnvironmentVariable("CORECLR_ENABLE_PROFILING");
            WriteEnvironmentVariable("CORECLR_PROFILER");
            WriteEnvironmentVariable("CORECLR_PROFILER_PATH");
            Greeting();
        }

        static void Greeting() => Console.WriteLine("Hello World!");

        static void WriteEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            Console.WriteLine($"{name} = {value}");
        }
    }
}

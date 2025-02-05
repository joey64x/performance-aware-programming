using System;
using System.Collections.Generic;

class PerformanceAwareProgramming
{
    private static readonly Dictionary<string, (string Description, Action<string[]> Run)> Homework = new()
    {
        ["01"] = ("Single Instruction Decode", SingleInstructionDecode.Run),
        ["02"] = ("Multi MOV Decode", MultiMoveDecode.Run)
    };

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- <homework_number> [homework_args]");
            Console.WriteLine("Available homework:");
            foreach (var hw in Homework)
            {
                Console.WriteLine($"  {hw.Key}: {hw.Value.Description}");
            }
            return;
        }

        if (Homework.TryGetValue(args[0], out var homework))
        {
            homework.Run(args[1..]);
        }
        else
        {
            Console.WriteLine($"Unknown homework number: {args[0]}");
        }
    }
}
using System;

static class SingleInstructionDecode
{
    private static readonly Dictionary<int, string> Registers16 = new Dictionary<int, string>
    {
        { 0b000, "ax" },
        { 0b001, "cx" },
        { 0b010, "dx" },
        { 0b011, "bx" },
        { 0b100, "sp" },
        { 0b101, "bp" },
        { 0b110, "si" },
        { 0b111, "di" }
    };
        
    private static readonly Dictionary<int, string>  Registers8 = new Dictionary<int, string>
    {
        { 0b000, "al" },
        { 0b001, "cl" },
        { 0b010, "dl" },
        { 0b011, "bl" },
        { 0b100, "ah" },
        { 0b101, "ch" },
        { 0b110, "dh" },
        { 0b111, "bh" }
    };

    public static void Run(string[] args)
    {
        Console.WriteLine("; 8086 Instruction Decode Simulation");
        Console.WriteLine("; ==================================\n");

        if (args.Length < 1)
        {
            Console.WriteLine("ERROR! Usage: dotnet run -- <pathToInstructionFile>");
            return;
        }

        ProcessInstructionFile(args[0]);
    }

    private static void CheckFileSize(FileInfo fileInfo)
    {
        if (fileInfo.Length > 10 * 1024 * 1024)
        {
            Console.WriteLine("WARNING: You are attempting to load in a file that is larger than 10MB, this could take a while on slower computers.");
        }
    }

    private static void ProcessInstructionFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        CheckFileSize(fileInfo);
        
        // write  out a header so we can save out a .asm file
        Console.WriteLine($"; {fileInfo.Name} disassembly:");
        Console.WriteLine("bits 16\n");

        var fileBytes = File.ReadAllBytes(filePath);

        var offset = 0;
        while (offset + 2 <= fileBytes.Length)
        {                
            // get the next 2 bytes and process them (2 bytes per instruction)
            var bytes = fileBytes.AsSpan(offset, 2);
            var opCode = bytes[0] >> 2;
            if (opCode == 0b100010) // mov instruction
            {
                DecodeMovInstruction(bytes);
            }
            else
            {
                Console.WriteLine($"Could not identify OpCode {opCode}");
            }

            offset += 2;
        }
    }

    private static void DecodeMovInstruction(ReadOnlySpan<byte> bytes)
    {
        var dBit = (bytes[0] & 0b00000010) >> 1;
        var wBit = bytes[0] & 0b00000001;
            
        // read the second byte in the instruction file
        // includes the mode, register and the register/memory
        var mod = bytes[1] >> 6;
        var reg = (bytes[1] & 0b00111000) >> 3;
        var rm = bytes[1] & 0b00000111;

        // use either the 8-bit or 16-bit registers based on the setting on the w bit
        var registerLookup = (wBit == 1) ? Registers16 : Registers8;
                
        // set the source and destination accordingly based on the d bit setting
        var dest = (dBit == 1) ? reg : rm;
        var src = (dest == reg) ? rm : reg;
                
        Console.WriteLine($"mov {registerLookup[dest]}, {registerLookup[src]}");
    }
}
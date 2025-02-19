using System;

static class MultiMoveDecode
{
    // Word registers
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
        
    // Byte registers
    private static readonly Dictionary<int, string> Registers8 = new Dictionary<int, string>
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

    private static readonly Dictionary<int, string> EffectiveAddresses = new Dictionary<int, string>
    {
        { 0b000, "bx + si" },
        { 0b001, "bx + di" },
        { 0b010, "bp + si" },
        { 0b011, "bp + di" },
        { 0b100, "si" },
        { 0b101, "di" },
        { 0b110, "bp" },
        { 0b111, "bx" }
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
        
        Console.WriteLine($"; {fileInfo.Name} disassembly:");
        Console.WriteLine("bits 16\n");

        var fileBytes = File.ReadAllBytes(filePath);
        var offset = 0;

        while (offset < fileBytes.Length)
        {                
            var firstByte = fileBytes[offset];

            // try to get the next 6 bytes, but if we don't have 6 bytes left in the file, send what we have
            int bytesLeft = fileBytes.Length - offset;
            int bytesToTake = Math.Min(6, bytesLeft);
            var instructionBytes = fileBytes.AsSpan(offset, bytesToTake);
            //OutputByteData(instructionBytes);

            var bytesUsed = 0;

            if (firstByte >> 2 == 0b100010) // Register/memory to/from register mov
            {
                bytesUsed += DecodeRegisterMemory(instructionBytes);
            }
            else if (firstByte >> 4 == 0b1011) // immediate to register mov
            {
                bytesUsed += ImmediateToRegisterMove(instructionBytes);
            }
            else if (firstByte >> 1 == 0b1100011) // Immediate to memory mov
            {
                bytesUsed += ImmediateToMemoryMove(instructionBytes);
            }
            else if (firstByte == 0b10100010 || firstByte == 0b10100011) // mov accumulator to memory
            {
                bytesUsed += AccumulatorToMemoryMove(instructionBytes);
            }
            else if (firstByte == 0b10100000 || firstByte == 0b10100001) // mov memory to accumulator
            {
                bytesUsed += MemoryToAccumulatorMove(instructionBytes);
            }
            else
            {
                Console.WriteLine($"Could not identify OpCode in byte {firstByte:B8}");
                bytesUsed++; // Skip unknown instruction byte
            }

            // Console.WriteLine($"Used {bytesUsed} bytes of the 4 bytes processed above. Will re-process {4 - bytesUsed} bytes.");
            offset += bytesUsed;
        }
    }

    private static void OutputByteData(ReadOnlySpan<byte> bytes)
    {
        // Console.Write("\n\n\nprocessing bytes: ");
        foreach (var b in bytes)
        {
            Console.Write($"0x{b:B8}, ");
        }
        Console.WriteLine();
    }

    private static short Get16BitValue(byte byte1, byte byte2) // x86 is little endian so the second byte is the most significant
    {        
        // Shift the second byte left by 8 bits and OR it with the first byte
        return (short)((byte2 << 8) | byte1);
    }

    // returns how many bytes were used in this instruction
    private static int ImmediateToRegisterMove(ReadOnlySpan<byte> bytes)
    {
        var wBit = (bytes[0] & 0b00001000) >> 3;
        var reg = bytes[0] & 0b00000111;

        if (wBit == 1)
        {
            // 16-bit value, need to get the value from the next 2 bytes
            Console.WriteLine($"mov { Registers16[reg] }, { Get16BitValue(bytes[1], bytes[2]) }");
            return 3;
        }
        else
        {
            // Console.WriteLine("; 8-bit immediate-to-register. wBit is " + wBit);
            Console.WriteLine($"mov { Registers8[reg] }, { (sbyte)bytes[1] }");
            return 2;
        }
    }

    private static int DecodeRegisterMemory(ReadOnlySpan<byte> bytes)
    {
        var dBit = (bytes[0] & 0b00000010) >> 1;
        var wBit = bytes[0] & 0b00000001;
            
        // read the second byte in the instruction file
        // includes the mode, register and the register/memory
        var mod = bytes[1] >> 6;
        var reg = (bytes[1] & 0b00111000) >> 3;
        var rm = bytes[1] & 0b00000111;

        if (mod == 0b11)
        {
            // register to register - no memory access
            RegisterToRegisterMove(dBit, wBit, reg, rm);
            return 2; // Register-to-register always takes 2 bytes
        }
        else
        {
            // memory mode - method handles displacement

            // check for special case first:
            if (mod == 0b00 && rm == 0b110) // Direct addressing
            {
                var addr = Get16BitValue(bytes[2], bytes[3]);
                var rmValue = $"[{addr}]";
                var regValue = (wBit == 1) ? Registers16[reg] : Registers8[reg];

                if (dBit == 1)
                {
                    Console.WriteLine($"mov {regValue}, {rmValue}");
                }
                else
                {
                    Console.WriteLine($"mov {rmValue}, {regValue}");
                }
                
                return 4;  // opcode + modrm + addr_lo + addr_hi
            }   

            return MemoryInstructionMove(mod, dBit, wBit, reg, rm, bytes[2], bytes[3]);
        }
    }

    private static void RegisterToRegisterMove(int dBit, int wBit, int reg, int rm)
    {
        // use either the 8-bit or 16-bit registers based on the setting on the w bit
        var registerLookup = (wBit == 1) ? Registers16 : Registers8;

        // set the source and destination accordingly based on the d bit setting
        var dest = (dBit == 1) ? reg : rm;
        var src = (dest == reg) ? rm : reg;

        Console.WriteLine($"mov {registerLookup[dest]}, {registerLookup[src]}");
    }

    private static int MemoryInstructionMove(int mod, int dBit, int wBit, int reg, int rm, byte dispLo, byte dispHi)
    {
        string regValue = (wBit == 1) ? Registers16[reg] : Registers8[reg];;
        string rmValue = "";

        int bytesUsed = 2;  // at least 2 bytes used - [MOV + d + w] and [mod + reg + r/m]

        switch (mod)
        {
            case 0b00:  // No displacement                
                rmValue = $"[{EffectiveAddresses[rm]}]";
                break;
            case 0b01: // 8-bit displacement
                var disp8 = (sbyte)dispLo;
                // decide what sign to show and space it properly
                var sign8 = disp8 < 0 ? "- " : "+ ";
                var value8 = Math.Abs((int)disp8);    // Use absolute value for display for formatting purposes
                rmValue = $"[{EffectiveAddresses[rm]} {sign8}{value8}]";
                bytesUsed += 1; // extra byte for DISP-LO
                break;
            case 0b10: // 16-bit displacement
                var disp16 = Get16BitValue(dispLo, dispHi);
                // decide what sign to show and space it properly
                var sign16 = disp16 < 0 ? "- " : "+ ";
                var value16 = Math.Abs((int)disp16);   // Use absolute value for display for formatting purposes
                rmValue = $"[{EffectiveAddresses[rm]} {sign16}{value16}]";
                bytesUsed += 2; // extra two bytes for DISP-LO and DISP-HI
                break;
        }

        if (dBit == 1) // REG field holds the destination
        {
            Console.WriteLine($"mov {regValue}, {rmValue}");
        }
        else // REG field holds the source
        {
            Console.WriteLine($"mov {rmValue}, {regValue}");
        }

        return bytesUsed;
    }

    private static int ImmediateToMemoryMove(ReadOnlySpan<byte> bytes)
    {
        var wBit = bytes[0] & 0b00000001;
        var mod = bytes[1] >> 6;
        var rm = bytes[1] & 0b00000111;
        
        int bytesUsed = 2;  // First two bytes
        string rmValue = "";
        
        if (mod == 0b00 && rm == 0b110) // Direct address
        {
            var addr = Get16BitValue(bytes[2], bytes[3]);
            rmValue = $"[{addr}]";
            bytesUsed += 2;
        }
        else
        {
            // Handle displacement similar to your existing code
            switch (mod)
            {
                case 0b00:
                    rmValue = $"[{EffectiveAddresses[rm]}]";
                    break;
                case 0b01:
                    rmValue = $"[{EffectiveAddresses[rm]} + {(sbyte)bytes[2]}]";
                    bytesUsed += 1;
                    break;
                case 0b10:
                    rmValue = $"[{EffectiveAddresses[rm]} + {Get16BitValue(bytes[2], bytes[3])}]";
                    bytesUsed += 2;
                    break;
            }
        }
        
        // Get the immediate value after the addressing bytes
        var immediate = wBit == 1 ? 
            Get16BitValue(bytes[bytesUsed], bytes[bytesUsed + 1]) :
            (sbyte)bytes[bytesUsed];
        
        bytesUsed += wBit == 1 ? 2 : 1;
        
        var sizeSpec = wBit == 1 ? "word" : "byte";
        Console.WriteLine($"mov {rmValue}, {sizeSpec} {immediate}");
        
        return bytesUsed;
    }

    private static int AccumulatorToMemoryMove(ReadOnlySpan<byte> bytes)
    {
        var wBit = bytes[0] & 0b00000001;
        var addr = Get16BitValue(bytes[1], bytes[2]);
        var reg = wBit == 1 ? "ax" : "al";
        
        Console.WriteLine($"mov [{addr}], {reg}");
        return 3;
    }

    private static int MemoryToAccumulatorMove(ReadOnlySpan<byte> bytes)
    {
        var wBit = bytes[0] & 0b00000001;
        var addr = Get16BitValue(bytes[1], bytes[2]);
        var reg = wBit == 1 ? "ax" : "al";
        
        Console.WriteLine($"mov {reg}, [{addr}]");
        return 3;
    }
}
local InstructionType = {
	MOV = "mov",
}

local InstructionPatterns = {
	[0b100010] = InstructionType.MOV,
}

local function getInstructionType(byte)
	byte = string.byte(byte)
	local typeBits = bit.rshift(byte, 2) -- instruction type is in the first 6 bits
	return InstructionPatterns[typeBits] or "Unknown instruction"
end

-- ensure we have a file to load from the arguments
if #arg < 1 then
	print("Usage: decoder.lua <filepath>")
end

local file = assert(io.open(arg[1], "rb"))

while true do
	-- Assuming a 16 bit instruction size (2 bytes to read in)

	-- Read the first byte in the instruction
	local byte1 = file:read(1)
	if not byte1 then
		break
	end

	local instrType = getInstructionType(byte1)
	print(string.byte(byte1) .. " is " .. instrType)

	-- Read the second byte in the instruction
	local byte2 = file:read(1)
	if not byte2 then
		break
	end

	print(string.byte(byte2))
end

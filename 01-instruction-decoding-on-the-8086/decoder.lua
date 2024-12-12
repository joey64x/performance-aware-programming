local OpCode = {
	[0b100010] = "mov",
}

local function getOpCode(byte)
	local typeBits = bit.rshift(byte, 2) -- instruction type is in the first 6 bits
	return OpCode[typeBits] or ("Unknown OPCODE" .. byte)
end

local registers16bit = {
  [0b000] = "ax",
  [0b001] = "cx",
  [0b010] = "dx",
  [0b011] = "bx",
  [0b100] = "sp",
  [0b101] = "bp",
  [0b110] = "si",
  [0b111] = "di",
}

local registers8bit = {
  [0b000] = "al",
  [0b001] = "cl",
  [0b010] = "dl",
  [0b011] = "bl",
  [0b100] = "ah",
  [0b101] = "ch",
  [0b110] = "dh",
  [0b111] = "bh",
}

-- ensure we have a file to load from the arguments
if #arg < 1 then
	print("Usage: decoder.lua <filepath>")
end

local file = assert(io.open(arg[1], "rb"))
-- Assuming a 16 bit instruction size (2 bytes to read in)
print(";" .. arg[1]:match("([^/]+)$") .. " disassembly:")
print("bits 16")

while true do

	-- Read the first byte in the instruction
	local byte1Str = file:read(1)
	if not byte1Str then
		break
	end

  -- print("Processing first byte...")
  local byte1 = string.byte(byte1Str)

  -- get the instruction type from the first byte
	local instrType = getOpCode(byte1)
	-- print(byte1 .. " is " .. instrType)

  -- store the d and w flags from the first byte
  local D = bit.band(bit.rshift(byte1, 1), 1)
  local W = bit.band(byte1, 1)
  -- print("d flag is " .. D .. " and w flag is " .. W)

  -- print("")
  -- print("Processing second byte...")
	-- Read the second byte in the instruction
	local byte2Str = file:read(1)
	if not byte2Str then
		break
	end

	local byte2 = string.byte(byte2Str)
  local MOD = bit.rshift(byte2, 6) -- MOD unused because we're always doing register to register right now
  local REG = bit.band(bit.rshift(byte2, 3), 0b00000111)
  local R_M = bit.band(byte2, 0b00000111)

  -- print("MOD is " .. MOD .. ", REG is " .. REG .. ", R/M is " .. R_M)

  -- output register to register instruction
  local registers = (W == 1) and registers16bit or registers8bit
  local destination = (D == 1) and REG or R_M
  local source = (destination == REG) and R_M or REG
  print(instrType.. " " .. registers[destination] .. ", " .. registers[source])
end

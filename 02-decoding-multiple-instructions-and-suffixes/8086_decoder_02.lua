local OpCode = {
	MOV = "mov",
}

local registersWord = {
  [0b000] = "ax",
  [0b001] = "cx",
  [0b010] = "dx",
  [0b011] = "bx",
  [0b100] = "sp",
  [0b101] = "bp",
  [0b110] = "si",
  [0b111] = "di",
}

local registersByte = {
  [0b000] = "al",
  [0b001] = "cl",
  [0b010] = "dl",
  [0b011] = "bl",
  [0b100] = "ah",
  [0b101] = "ch",
  [0b110] = "dh",
  [0b111] = "bh",
}

local effectiveAddresses = {
  [0b000] = "bx + si",
  [0b001] = "bx + di",
  [0b010] = "bp + si",
  [0b011] = "bp + di",
  [0b100] = "si",
  [0b101] = "di",
  [0b110] = "bp",
  [0b111] = "bx",
}

local function moveRegisterToRegister(W, D, REG, R_M)
  -- print("MOD is " .. MOD .. ", REG is " .. REG .. ", R/M is " .. R_M)
  -- output register to register instruction
  local registers = (W == 1) and registersWord or registersByte
  local destination = (D == 1) and REG or R_M -- When D is 1, the Register is the destination, if not it's the Register/Memory field
  local source = (destination == REG) and R_M or REG
  print(OpCode.MOV .. " " .. registers[destination] .. ", " .. registers[source])
end

local function get16bitValue(bytes)
  return string.byte(bytes, 1) + (string.byte(bytes, 2) * 256)
end

local function getEffAddrCalculation(value, MOD, file)
  if (value == effectiveAddresses[0b110]) then
    if (MOD == 0b00) then
      return get16bitValue(file:read(2))
    else
      return value
    end
  elseif (MOD == 0b01) then
    return value .. " + " .. string.byte(file:read(1))
  elseif (MOD == 0b10) then
    return value .. " + " .. get16bitValue(file:read(2))
  else
    return value
  end
end

local function moveMemory(W, D, REG, R_M, MOD, file)
  local destination = ""
  local source = ""

  if (D == 1) then
    destination = (W == 1) and registersWord[REG] or registersByte[REG]
    source = "[" .. getEffAddrCalculation(effectiveAddresses[R_M], MOD, file) .. "]"
  else
    source = (W == 1) and registersWord[REG] or registersByte[REG]
    destination = "[" .. getEffAddrCalculation(effectiveAddresses[R_M], MOD, file) .. "]"
  end

  print(OpCode.MOV .. " " .. destination .. ", " .. source)
end

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

  local byte1 = string.byte(byte1Str)

  if (bit.rshift(byte1, 4) == 0b1011) then
    -- immediate to register mode
    -- print("; immediate to register")
    local REG = bit.band(byte1, 0b00000111)
    local W = bit.band(byte1, 0b00001000)
    local immInstr = OpCode.MOV .. " "

    if (W == 0b0) then
      immInstr = immInstr .. registersByte[REG] .. ", " .. string.byte(file:read(1))
    else
      immInstr = immInstr .. registersWord[REG] .. ", " .. get16bitValue(file:read(2))
    end
    print(immInstr)
  elseif (bit.rshift(byte1, 2) == 0b100010) then
    -- register/memory to/from register mode
    local D = bit.band(bit.rshift(byte1, 1), 1)
    local W = bit.band(byte1, 1) -- tells us if the instruction operates on byte (0) or word (1) data

    local byte2Str = file:read(1)
  	if not byte2Str then
  		break
  	end

  	local byte2 = string.byte(byte2Str)
    local MOD = bit.rshift(byte2, 6) -- tells us whether or not we have a displacement
    local REG = bit.band(bit.rshift(byte2, 3), 0b00000111)
    local R_M = bit.band(byte2, 0b00000111)

    if (MOD == 0b11) then
      -- print("; register to register")
      moveRegisterToRegister(W, D, REG, R_M)
    else
      -- print("; source address calculation")
      moveMemory(W, D, REG, R_M, MOD, file)
    end
  end
end

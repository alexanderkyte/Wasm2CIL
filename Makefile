
all: test

.PHONY: test
test: Parser.exe factorial.wasm Makefile
	mono --debug Parser.exe factorial.wasm Factorial
	monodis FactorialProxy.dll

Parser.exe: Parser.cs Makefile Instruction.cs
	mcs -debug Parser.cs Instruction.cs

.PHONY: clean
clean:
	- rm -rf Parser.exe

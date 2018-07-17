
all: test

.PHONY: test
test: Parser.exe factorial.wasm Makefile
	mono --debug Parser.exe factorial.wasm

Parser.exe: Parser.cs Makefile
	mcs -debug Parser.cs Instruction.cs

.PHONY: clean
clean:
	- rm -rf Parser.exe

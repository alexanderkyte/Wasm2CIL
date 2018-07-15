
all: test

.PHONY: test
test: Parser.exe factorial.wasm
	mono Parser.exe factorial.wasm

Parser.exe: Parser.cs
	mcs Parser.cs

.PHONY: clean
clean:
	- rm -rf Parser.exe

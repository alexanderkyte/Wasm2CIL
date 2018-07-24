
all: test

.PHONY: test
test: dis

.PHONY: dis
dis: FactorialProxy.dll 
	monodis FactorialProxy.dll

FactorialProxy.dll: Parser.exe factorial.wasm Makefile
	mono --debug Parser.exe factorial.wasm Factorial

Parser.exe: Parser.cs Makefile Instruction.cs
	mcs -debug Parser.cs Instruction.cs

.PHONY: aot
aot: FactorialProxy.dll
	mono --aot=llvmonly,asmonly,llvm-outfile=tmp.bc FactorialProxy.dll

.PHONY: verify
verify: FactorialProxy.dll
	mono --verify-all FactorialProxy.dll

.PHONY: run
run: FactorialProxy.dll
	mono --interpreter FactorialProxy.dll

.PHONY: clean
clean:
	- rm -rf Parser.exe

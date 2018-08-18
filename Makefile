
all: test

.PHONY: test
test: dis

.PHONY: dis
dis: FactorialProxy.dll 
	monodis FactorialProxy.dll

FactorialProxy.dll: Wasm2CIL.exe factorial.wasm Makefile
	mono --debug Wasm2CIL.exe factorial.wasm FactorialProxy

Wasm2CIL.exe: Makefile Parser.cs Instruction.cs WebassemblyModule.dll Embedder.cs
	mcs -debug Parser.cs Instruction.cs Embedder.cs -r:WebassemblyModule.dll -o Wasm2CIL.exe

.PHONY: aot
aot: FactorialProxy.dll
	mono --aot=llvmonly,asmonly,llvm-outfile=tmp.bc FactorialProxy.dll

.PHONY: verify
verify: FactorialProxy.dll
	mono --verify-all FactorialProxy.dll

.PHONY: run
run: FactorialProxy.dll
	echo "Input 1, Expected: 1"
	mono --interpreter FactorialProxy.dll 1
	echo "Input 2, Expected: 2"
	mono --interpreter FactorialProxy.dll 2
	echo "Input 3, Expected: 6"
	mono --interpreter FactorialProxy.dll 3
	echo "Input 4, Expected: 24"
	mono --interpreter FactorialProxy.dll 4
	echo "Input 5, Expected: 120"
	mono --interpreter FactorialProxy.dll 5

.PHONY: clean
clean:
	- rm -rf Wasm2CIL.exe

WebassemblyModule.dll: WebassemblyModule.cs
	mcs -t:library WebassemblyModule.cs

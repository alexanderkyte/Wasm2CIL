
all: test

.PHONY: test
test: dis

.PHONY: dis-if
dis-if: IfProxy.dll 
	monodis IfProxy.dll

.PHONY: dis-dsp
dis-dsp: DSPProxy.dll 
	monodis FactorialProxy.dll

IfProxy.dll: if.wasm Makefile
	mono --debug ../../Parser.exe if.wasm If

DSPProxy.dll: webdsp_c.wasm Makefile
	mono --debug ../../Parser.exe webdsp_c.wasm DSP



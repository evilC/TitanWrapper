#SingleInstance force

; Load CLR library that allows us to load C# DLLs
#include CLR.ahk

; Instantiate class from C# DLL
asm := CLR_LoadLibrary("TitanWrapper.dll")
global titan := asm.CreateInstance("TitanWrapper")

titan.SubscribeButton(1, Func("ButtonEvent"))

ButtonEvent(state){
	titan.SetButton(2, state)
}

^Esc::
	ExitApp
#SingleInstance force

; Load CLR library that allows us to load C# DLLs
#include CLR.ahk

; Instantiate class from C# DLL
asm := CLR_LoadLibrary("TitanWrapper.dll")
global titan := asm.CreateInstance("TitanWrapper.Wrapper")

; Subscribe to some buttons and axes on the Titan input port
titan.SubscribeButton(1, Func("ButtonEvent"))
titan.SubscribeAxis(1, Func("AxisEvent"))

ButtonEvent(state){
	; Press button 2 on Titan controller
	titan.SetButton(2, state)
}

AxisEvent(state){
	; Move axis 2 on Titan controller
	titan.SetAxis(2, state)
}

^Esc::
	ExitApp
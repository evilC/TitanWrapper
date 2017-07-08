# TitanWrapper
A C# wrapper for the Titan One API, with sample AutoHotkey scripts

## Usage
### Setup
#### C#
Reference the DLL in your project, then:  
```
// Instantiate the class
var titan = new TitanWrapper.Wrapper();
```
#### Autohotkey
Load the DLL using the include CLR library by Lexikos:
```
; Load CLR library that allows us to load C# DLLs
#include CLR.ahk
; Instantiate class from C# DLL
asm := CLR_LoadLibrary("TitanWrapper.dll")
global titan := asm.CreateInstance("TitanWrapper.Wrapper")
```

### Button and Axis Numbers:
| Button Number |    PS3   |   PS4    | XB360 |  XB1  |
|---------------|----------|----------|-------|-------|
| 1             | Cross    | Cross    | A     | A     |
| 2             | Circle   | Circle   | B     | B     |
| 3             | Square   | Square   | X     | X     |
| 4             | Triangle | Triangle | Y     | Y     |
| 5             |  L1      | L1       | LB    | LB    |
| 6             |  R1      | R1       | RB    | RB    |
| 7             |  L2      | L2       | LS    | LS    |
| 8             |  R2      | R2       | RS    | RS    |
| 9             |  L3      | L3       | Back  | Back  |
| 10            |  R3      | R3       | Start | Start |
| 11            | Select   | Share    | Xbox  | Xbox  |
| 12            |  Start   | Options  |       |       |
| 13            |   PS     |  PS      |       |       |
| 14            |          | Touch    |       |       |

### Setting the state of outputs
#### C# and Autohotkey
```
titan.SetButton(1, value);
titan.SetAxis(1, value);
```

### Subscribing to inputs
#### C#
```
titan.SubscribeButton(1, new Action<int>((value) => {
    Console.WriteLine("Button 1 Value: " + value);
}));

titan.SubscribeAxis(1, new Action<int>((value) => {
    Console.WriteLine("Axis 1 Value: " + value);
}));
```

#### Autohotkey
```
titan.SubscribeButton(1, Func("ButtonEvent"))
titan.SubscribeAxis(1, Func("AxisEvent"))

ButtonEvent(state){
	Tooltip % "Button 1 Value: " state
}

AxisEvent(state){
	Tooltip % "Axis 1 Value: " state
}
```
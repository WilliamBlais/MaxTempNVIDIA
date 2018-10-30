# MaxTempNVIDIA
MaxTempNVIDIA - Fan Speed Control Based On Max Temperature

# Usage
Simply execute MaxTempNVIDIA.exe, it will detect all your GPUs and set the max temperature to 70°C.

To specify a custom temperature, use the following bat file:
```
@echo off
:start
MaxTempNVIDIA.exe 65 70 30
goto start
```
Where 65 is the temperature in °C for the GPU0, 70 °C for the GPU1...

# Credits
https://github.com/TwistedMexi/CudaManager
https://github.com/LibreHardwareMonitor/LibreHardwareMonitor

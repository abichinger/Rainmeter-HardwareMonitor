HardwareMonitor
===============

HardwareMonitor is a plugin for Rainmeter and uses the OpenHardwareMonitorLibary to monitor sensors of your computer. 

Features :

- Temperatures
- Loads
- Clocks
- Fanspeed
- Memory Usage
- ...

Install :

1. Download Open Hardware Monitor: http://openhardwaremonitor.org/downloads/
2. then copy the file OpenHardwareMonitorLib.dll into your Rainmeter folder next to Rainmeter.exe
3. Compile the HardwareMonitor.dll yourself or use the .dll file inside the x64/x32 folder.
4. copy the HardwareMonitor.dll into the Plugins folder.
5. you need to run Rainmeter as admin otherwise the Open Hardware Monitor Libary won't work.

Example:
>[CPUTemp]  
Measure=Plugin  
Plugin=HardwareMonitor.dll  
Type=cpu temp "CPU Package"
MinValue=0  
MaxValue=100  

Allowed Types :

Type = hardware "hardwarename" sensor "sensorname"

- hardware: cpu, gpu, hdd, ram and mainboard
- sensor: temp, load, clock, fan, power, control, data and voltage
- hardwarename and sensorname (optional): names are diplayed in OpenHardwareMonitor
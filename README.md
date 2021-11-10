Rainmeter OpenHardwareMonitor Plugin
===============

This Plugin allowes Rainmeter measures to access the sensor data of [OpenHardwareMonitor](http://openhardwaremonitor.org). The data is fetched from WMI.

## Requirements

- [OpenHardwareMonitor](http://openhardwaremonitor.org) is running

## Install

1. Download the latest [Release](https://github.com/abichinger/Rainmeter-HardwareMonitor/releases)
2. Install the .rmskin file

## Measure

### Usage:

```ini
[Measure]  
Measure=Plugin  
Plugin=OpenHardwareMonitor.dll  
HardwareType=Mainboard | SuperIO | CPU | GpuNvidia | GpuAti | TBalancer | Heatmaster | HDD
HardwareName=HardwareName
HardwareIndex=HardwareIndex
SensorType=Voltage | Clock | Temperature | Load | Fan | Flow | Control | Level
SensorName=SensorName
SensorIndex=SensorIndex
```

### Supported parameters

| Parameter | Description | Default |
| --- | --- | --- |
| [HardwareType](http://openhardwaremonitor.org/wordpress/wp-content/uploads/2011/04/OpenHardwareMonitor-WMI.pdf) | type of hardware | empty string |
| HardwareName | name of hardware | empty string |
| HardwareIndex | index of hardware, if multiple devices match the supplied hardware filter | 0 |
| [SensorType](http://openhardwaremonitor.org/wordpress/wp-content/uploads/2011/04/OpenHardwareMonitor-WMI.pdf) | type of sensor | empty string |
| SensorName | name of sensor | empty string |
| SensorIndex | index of hardware, if multiple devices match the supplied filter | 0 |

each parameter is **optional**

### Examples ###

![Open Hardware Monitor GPU](assets/gpu_core_load.png)

The following examples show how to measure the GPU Core Load 

```ini
[GPUCoreLoad]  
Measure=Plugin  
Plugin=OpenHardwareMonitor.dll  
HardwareType=GpuAti
SensorType=Load
SensorName=GPU Core
MinValue=0  
MaxValue=100  

[GPUCoreLoadAlternative]  
Measure=Plugin  
Plugin=OpenHardwareMonitor.dll  
HardwareName=AMD Radeon RX 5700 XT
SensorType=Load
SensorName=GPU Core
MinValue=0  
MaxValue=100  
```

More examples can be found inside the skin files [cpu.ini](Skins/CPU/cpu.ini) and [gpu.ini](Skins/GPU/gpu.ini).
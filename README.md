Rainmeter LibreHardwareMonitor Plugin
===============

This Plugin allowes Rainmeter measures to access the sensor data of [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

# Requirements

[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) is running.
Make sure to activate the built-in web server (Options -> Remote Web Server -> Run) 

# Install

1. Download the latest [Release](https://github.com/abichinger/LibreMeter/releases)
2. Install the .rmskin file

# Measure

## Usage:

```ini
[Measure]  
Measure=Plugin  
Plugin=LibreMeter
SensorId=/amdcpu/0/load/2
```

## Supported parameters

| Parameter | Description | Default |
| --- | --- | --- |
| SensorId | Open http://localhost:8085/metrics to find a sensor's id |  |
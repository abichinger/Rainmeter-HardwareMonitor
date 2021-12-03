$ErrorActionPreference = "Stop"
$nextRelease = $args[0]

node .\scripts\update_version.js $nextRelease
if ($LASTEXITCODE -ne 0) { throw "Exit code is $LASTEXITCODE" }

msbuild.exe -target:Build -p:"Configuration=Release;Platform=x86" .\OpenHardwareMonitor\
if ($LASTEXITCODE -ne 0) { throw "Exit code is $LASTEXITCODE" }

msbuild.exe -target:Build -p:"Configuration=Release;Platform=x64" .\OpenHardwareMonitor\
if ($LASTEXITCODE -ne 0) { throw "Exit code is $LASTEXITCODE" }

node .\scripts\gen_rmskin.js OpenHardwareMonitor_$nextRelease.rmskin
if ($LASTEXITCODE -ne 0) { throw "Exit code is $LASTEXITCODE" }
const ini = require('ini')
const fs = require('fs')

nextRelease = process.argv[2]
rmskinINI = './RMSKIN.ini'
asseblyInfo = "./OpenHardwareMonitor/AssemblyInfo.cs"
csprojPath = "./OpenHardwareMonitor/OpenHardwareMonitor.csproj"

// update RMSKIN.ini version
var config = ini.parse(fs.readFileSync(rmskinINI, 'utf-8'))
config.rmskin.Version = nextRelease
fs.writeFileSync(rmskinINI, ini.stringify(config))

// update AsseblyInfo.cs
var info = fs.readFileSync(asseblyInfo, 'utf-8')
info = info.replace(/AssemblyVersion\(".*"\)/gi, `AssemblyVersion("${nextRelease}")`)
fs.writeFileSync(asseblyInfo, info)

// update OpenHardwareMonitor.csproj
var csproj = fs.readFileSync(csprojPath, 'utf-8')
csproj = csproj.replace(/<ApplicationVersion>.*<\/ApplicationVersion>/gi, `<ApplicationVersion>${nextRelease}</ApplicationVersion>`)
fs.writeFileSync(csprojPath, csproj)


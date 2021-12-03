const jspack = require('jspack').jspack;
const chr = (n) => String.fromCharCode(n)
const fs = require('fs')
const archiver = require('archiver')

let rmskinPath = process.argv[2]

//create zip
let rmskin = archiver('zip')
let rmskinOutput = fs.createWriteStream(rmskinPath)

rmskinOutput.on('close', function() {
    console.log(rmskin.pointer() + ' total bytes');

    //prepare footer
    let rmskinStats = fs.statSync(rmskinPath)

    let key = 'RMSKIN\x00'
    let fileSize = rmskinStats.size
    let flags = chr(0)

    console.log('file size:',fileSize)

    let bytes = jspack.Pack('<lxxxxB7s', [fileSize, flags, key])
    let footer = Buffer.from(bytes)
    console.log(footer.length, footer)

    fs.appendFileSync(rmskinPath, footer)
});

rmskin.pipe(rmskinOutput)

rmskin.file('./RMSKIN.ini', {name: 'RMSKIN.ini'})
rmskin.directory('./Skins/', 'Skins/OHM')
rmskin.file('build/x32/Release/OpenHardwareMonitor.dll', {name: 'Plugins/32bit/OpenHardwareMonitor.dll'})
rmskin.file('build/x64/Release/OpenHardwareMonitor.dll', {name: 'Plugins/64bit/OpenHardwareMonitor.dll'})

rmskin.finalize()
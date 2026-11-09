$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolsFolder = Join-Path $projectRoot '.tools'
$pythonPath = Join-Path $toolsFolder 'python310/python.exe'
$environmentPython = Join-Path $projectRoot '.venv-mlagents/Scripts/python.exe'
New-Item -ItemType Directory -Force $toolsFolder | Out-Null
if (!(Test-Path $pythonPath)) {
    $installerPath = Join-Path $toolsFolder 'python-3.10.11-amd64.exe'
    Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe' -OutFile $installerPath
    $targetFolder = Join-Path $toolsFolder 'python310'
    $installerArgs = @('/quiet','InstallAllUsers=0',('TargetDir="' + $targetFolder + '"'),'Include_launcher=0','InstallLauncherAllUsers=0','PrependPath=0','Include_test=0','Include_doc=0','Include_tcltk=0','AssociateFiles=0','Shortcuts=0')
    $installation = Start-Process -FilePath $installerPath -ArgumentList $installerArgs -WindowStyle Hidden -PassThru -Wait
    if ($installation.ExitCode -notin @(0,3010)) { throw "Installation Python echouee : $($installation.ExitCode)" }
}
& $pythonPath -c 'import struct,sys; assert struct.calcsize("P")==8 and (3,10,1)<=sys.version_info[:3]<=(3,10,12), "Python 3.10 64 bits compatible requis"'
if ($LASTEXITCODE -ne 0) { throw 'Version Python incompatible.' }
if (!(Test-Path $environmentPython)) {
    & $pythonPath -m venv (Join-Path $projectRoot '.venv-mlagents')
    if ($LASTEXITCODE -ne 0) { throw 'Creation de l environnement Python echouee.' }
}
& $environmentPython -m pip install -r (Join-Path $PSScriptRoot 'requirements-lock.txt')
if ($LASTEXITCODE -ne 0) { throw 'Installation ML-Agents echouee.' }
& $environmentPython -m pip check
if ($LASTEXITCODE -ne 0) { throw 'Dependances Python incompatibles.' }
Write-Host 'Environnement ML-Agents pret. Consulter Training/README.md pour lancer les essais.'

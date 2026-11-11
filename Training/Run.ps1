param(
    [ValidateSet('connection','train','baseline','evaluate','model','summarize')]
    [string]$Mode = 'train',
    [string]$RunId = 'kart-first',
    [string]$Source = '',
    [ValidateSet('moo','mario')][string]$Track = 'moo',
    [int]$Episodes = 10,
    [int]$Steps = 0,
    [int]$Seed = 42,
    [switch]$Resume,
    [switch]$Graphics,
    [switch]$Editor,
    [ValidateSet('original','balanced')][string]$Profile = 'original',
    [int]$Goal = 6
)
$projectRoot = Split-Path $PSScriptRoot -Parent
$pythonPath = Join-Path $projectRoot '.venv-mlagents/Scripts/python.exe'
if (!(Test-Path $pythonPath)) { throw 'Environnement Python absent : consulter Training/README.md.' }
$runArgs = @((Join-Path $PSScriptRoot 'run.py'), $Mode, '--run-id', $RunId, '--track', $Track, '--episodes', $Episodes, '--seed', $Seed)
if ($Source) { $runArgs += @('--source', $Source) }
if ($Steps -gt 0) { $runArgs += @('--steps', $Steps) }
if ($Resume) { $runArgs += '--resume' }
if ($Graphics) { $runArgs += '--graphics' }
if ($Editor) { $runArgs += '--editor' }
$runArgs += @('--profile', $Profile, '--goal', $Goal)
& $pythonPath @runArgs
exit $LASTEXITCODE

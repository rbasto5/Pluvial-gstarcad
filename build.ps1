param([string]$cad='C:\Program Files\Gstarsoft\GstarCAD2025')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$sourceDir=if(Test-Path -LiteralPath 'src\Core.cs'){'src'}else{'.'}
$testSource=if(Test-Path -LiteralPath 'tests\CoreTests.cs'){'tests\CoreTests.cs'}else{'CoreTests.cs'}
New-Item -ItemType Directory -Force -Path dist,tests | Out-Null
if(Test-Path -LiteralPath 'D-Prumada-Plu.dwg'){Copy-Item -LiteralPath 'D-Prumada-Plu.dwg' -Destination 'dist\D-Prumada-Plu.dwg'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:library /platform:x64 /out:dist\Duall.Pluvial.1.1.dll /reference:"$cad\GcCoreMgd.dll" /reference:"$cad\GcDbMgd.dll" /reference:"$cad\GcMgd.dll" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Runtime.Serialization.dll "$sourceDir\Core.cs" "$sourceDir\Plugin.cs" "$sourceDir\Panel.cs"
if($LASTEXITCODE -ne 0) { throw 'Falha ao compilar plugin.' }
& $compiler /nologo /out:tests\CoreTests.exe /reference:System.Runtime.Serialization.dll "$sourceDir\Core.cs" $testSource
if($LASTEXITCODE -ne 0) { throw 'Falha ao compilar testes.' }
& .\tests\CoreTests.exe
if($LASTEXITCODE -ne 0) { throw 'Falha nos testes.' }

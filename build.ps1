param(
 [string]$cad='',
 [ValidateSet('2023','2025')][string]$target='2025'
)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
if(!$cad){$cad="C:\Program Files\Gstarsoft\GstarCAD$target"}
$sourceDir=if(Test-Path -LiteralPath 'src\Core.cs'){'src'}else{'.'}
$testSource=if(Test-Path -LiteralPath 'tests\CoreTests.cs'){'tests\CoreTests.cs'}else{'CoreTests.cs'}
$references=if($target -eq '2023'){@('gmap.dll','gmdb.dll')}else{@('GcCoreMgd.dll','GcDbMgd.dll','GcMgd.dll')}
foreach($name in $references){if(!(Test-Path -LiteralPath (Join-Path $cad $name))){throw "Referência ausente: $cad\$name. Informe -cad com a instalação ou SDK do GstarCAD $target."}}
New-Item -ItemType Directory -Force -Path dist,tests | Out-Null
if(Test-Path -LiteralPath 'D-Prumada-Plu.dwg'){Copy-Item -LiteralPath 'D-Prumada-Plu.dwg' -Destination 'dist\D-Prumada-Plu.dwg'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=if($target -eq '2023'){'dist\Duall.Pluvial.1.5.GC2023.dll'}else{'dist\Duall.Pluvial.1.5.GC2025.dll'}
$compileArgs=@('/nologo','/target:library','/platform:x64',"/out:$output")
if($target -eq '2023'){$compileArgs+='/define:GC2023'}
foreach($name in $references){$compileArgs+="/reference:$(Join-Path $cad $name)"}
$compileArgs+=@('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Runtime.Serialization.dll',"$sourceDir\Core.cs","$sourceDir\Plugin.cs","$sourceDir\Panel.cs")
& $compiler @compileArgs
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar plugin.'}
& $compiler /nologo /out:tests\CoreTests.exe /reference:System.Runtime.Serialization.dll "$sourceDir\Core.cs" $testSource
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar testes.'}
& .\tests\CoreTests.exe
if($LASTEXITCODE -ne 0){throw 'Falha nos testes.'}
Write-Output "Compilado para GstarCAD ${target}: $output"


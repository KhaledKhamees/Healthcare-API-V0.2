$p = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0_2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
$asm = [System.Reflection.Assembly]::LoadFrom($p)
$types = $asm.GetTypes()
$namespaces = $types | ForEach-Object { $_.Namespace } | Sort-Object -Unique
Write-Output "Namespaces found:"
$namespaces | ForEach-Object { Write-Output "  $_" }

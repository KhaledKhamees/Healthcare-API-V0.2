$p = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
if (Test-Path $p) {
    $asm = [System.Reflection.Assembly]::LoadFrom($p)
    $ns = $asm.GetTypes() | ForEach-Object { $_.Namespace } | Sort-Object -Unique
    Write-Output "Namespaces in dot Application DLL:"
    $ns | ForEach-Object { Write-Output "  $_" }
} else {
    Write-Output "Dot path DLL not found"
}

$p2 = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0_2_Application\bin\Debug\net8.0\Healthcare-API_V0_2_Application.dll'
if (Test-Path $p2) {
    $asm2 = [System.Reflection.Assembly]::LoadFrom($p2)
    $ns2 = $asm2.GetTypes() | ForEach-Object { $_.Namespace } | Sort-Object -Unique
    Write-Output "Namespaces in underscore Application DLL:"
    $ns2 | ForEach-Object { Write-Output "  $_" }
} else {
    Write-Output "Underscore path DLL not found"
}

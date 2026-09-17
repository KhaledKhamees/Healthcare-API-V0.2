$p = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
$asmName = [System.Reflection.AssemblyName]::GetAssemblyName($p)
Write-Output "Assembly Name: $($asmName.Name)"

# Try to load just the assembly name to check namespaces
$asmp = [System.Reflection.Assembly]::LoadFrom($p)
$ex = $null
try {
    $types = $asmp.GetTypes()
} catch {
    Write-Output "Error: $($_.Exception.Message)"
    if ($_.Exception.InnerExceptions) {
        foreach ($inner in $_.Exception.InnerExceptions) {
            Write-Output "Inner: $($inner.Message)"
        }
    }
}

# Just check the exported types info
Write-Output "Trying exported types..."
foreach ($ti in $asmp.ExportedTypes) {
    Write-Output "  $($ti.Namespace)"
}

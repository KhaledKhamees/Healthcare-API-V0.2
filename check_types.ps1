$dotApp = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
$underApp = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0_2_Application\bin\Debug\net8.0\Healthcare-API_V0_2_Application.dll'

function Check-Dll {
    param($path)
    if (Test-Path $path) {
        Write-Output "=== $path ==="
        $asm = [System.Reflection.Assembly]::LoadFrom($path)
        try {
            foreach ($t in $asm.GetTypes()) {
                if ($t.Name -like '*Repository*' -or $t.Name -like '*IPatient*' -or $t.Name -like '*IDoctor*') {
                    Write-Output "  $($t.Namespace).$($t.Name)"
                }
            }
        } catch {
            Write-Output "  Error loading types: $($_.Exception.Message)"
        }
    } else {
        Write-Output "NOT FOUND: $path"
    }
}

Check-Dll -path $dotApp
Check-Dll -path $underApp

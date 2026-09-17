$dotApp = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
$asm = [System.Reflection.Assembly]::LoadFrom($dotApp)
try {
    $asm.GetTypes() | ForEach-Object { Write-Output "OK: $($_.FullName)" }
} catch {
    Write-Output "Error: $($_.Exception.GetType().Name)"
    Write-Output "Message: $($_.Exception.Message)"
    if ($_.Exception.InnerException) {
        Write-Output "Inner: $($_.Exception.InnerException.Message)"
    }
    # Try LoaderExceptions
    $lex = $asm.GetTypes()
}

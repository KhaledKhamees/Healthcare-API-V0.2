$p = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
$asm = [System.Reflection.Assembly]::LoadFrom($p)
try {
    $types = $asm.GetTypes()
} catch {
    $ex = $_.Exception
    Write-Output "Exception: $($ex.GetType().FullName)"
    Write-Output "Message: $($ex.Message)"
    # Get LoaderExceptions from the assembly
    $prop = $asm.GetType().GetProperty('LoaderExceptions', [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
    if ($prop) {
        $lex = $prop.GetValue($asm)
        foreach ($e in $lex) {
            Write-Output "LOADER EXCEPTION: $($e.Message)"
        }
    }
}

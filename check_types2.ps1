$p = 'E:\Edu\Projects\Healthcare-API V0.2\Healthcare-API V0.2\Healthcare-API_V0.2_Application\bin\Debug\net8.0\Healthcare-API_V0.2_Application.dll'
try {
    $asm = [System.Reflection.Assembly]::LoadFrom($p)
    $types = $asm.GetTypes()
    foreach ($t in $types) {
        if ($t.Name -match 'Patient|Doctor|Clinic|Specialization|Appointment|IClinic|IDoctor|IPatient|ISpecialization|IAppointment') {
            Write-Output "$($t.Namespace).$($t.Name)"
        }
    }
} catch {
    Write-Output "Exception: $($_.Exception.GetType().FullName)"
    Write-Output "Message: $($_.Exception.Message)"
    if ($_.Exception.InnerException) {
        Write-Output "InnerException: $($_.Exception.InnerException.Message)"
    }
}

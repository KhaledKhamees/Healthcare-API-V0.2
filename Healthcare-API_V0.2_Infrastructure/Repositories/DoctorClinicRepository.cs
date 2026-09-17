using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class DoctorClinicRepository : IDoctorClinicRepository
{
    private readonly HealthcareDbContext _dbContext;

    public DoctorClinicRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DoctorClinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoctorClinics.AsNoTracking().FirstOrDefaultAsync(dc => dc.DoctorClinicId == id, cancellationToken);
    }

    public async Task<DoctorClinic?> GetByDoctorAndClinicAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoctorClinics.AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.DoctorId == doctorId && dc.ClinicId == clinicId, cancellationToken);
    }

    public async Task AddAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoctorClinics.AddAsync(doctorClinic, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default)
    {
        _dbContext.DoctorClinics.Update(doctorClinic);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DoctorClinic doctorClinic, CancellationToken cancellationToken = default)
    {
        _dbContext.DoctorClinics.Remove(doctorClinic);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

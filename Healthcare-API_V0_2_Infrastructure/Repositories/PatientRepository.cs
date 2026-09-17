using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly HealthcareDbContext _dbContext;

    public PatientRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == id, cancellationToken);
    }

    public async Task<Patient?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _dbContext.Patients.AddAsync(patient, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _dbContext.Patients.Update(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _dbContext.Patients.Remove(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


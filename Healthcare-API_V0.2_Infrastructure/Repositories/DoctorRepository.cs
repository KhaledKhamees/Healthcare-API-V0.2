using Healthcare_API.Models;
using Healthcare_API.Models.Enums;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly HealthcareDbContext _dbContext;

    public DoctorRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Doctors
            .Include(d => d.Specialization)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == id, cancellationToken);
    }

    public async Task<Doctor?> GetByApplicationUserIdAsync(Guid applicationUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Doctors
            .Include(d => d.Specialization)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ApplicationUserId == applicationUserId, cancellationToken);
    }

    public async Task<List<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Doctors
            .Include(d => d.Specialization)
            .AsNoTracking()
            .Where(d => d.SpecializationId == specializationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Doctors.AddAsync(doctor, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _dbContext.Doctors.Update(doctor);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _dbContext.Doctors.Remove(doctor);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class ClinicRepository : IClinicRepository
{
    private readonly HealthcareDbContext _dbContext;

    public ClinicRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.ClinicId == id, cancellationToken);
    }

    public async Task<List<Clinic>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clinics.AsNoTracking()
            .Where(c => c.City == city)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        await _dbContext.Clinics.AddAsync(clinic, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        _dbContext.Clinics.Remove(clinic);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


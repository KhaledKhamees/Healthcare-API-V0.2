using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class SpecializationRepository : ISpecializationRepository
{
    private readonly HealthcareDbContext _dbContext;

    public SpecializationRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Specialization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Specializations.AsNoTracking().FirstOrDefaultAsync(s => s.SpecializationId == id, cancellationToken);
    }

    public async Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Specializations.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task AddAsync(Specialization specialization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Specializations.AddAsync(specialization, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default)
    {
        _dbContext.Specializations.Update(specialization);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Specialization specialization, CancellationToken cancellationToken = default)
    {
        _dbContext.Specializations.Remove(specialization);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


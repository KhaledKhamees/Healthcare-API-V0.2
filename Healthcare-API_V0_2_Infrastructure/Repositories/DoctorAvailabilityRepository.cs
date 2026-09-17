using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.Interfaces.Repository;
using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2_Infrastructure.Repositories;

public class DoctorAvailabilityRepository : IDoctorAvailabilityRepository
{
    private readonly HealthcareDbContext _dbContext;

    public DoctorAvailabilityRepository(HealthcareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DoctorAvailability>> GetByDoctorClinicAsync(Guid doctorClinicId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoctorAvailabilities.AsNoTracking()
            .Where(da => da.DoctorClinicId == doctorClinicId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DoctorAvailability availability, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoctorAvailabilities.AddAsync(availability, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DoctorAvailability availability, CancellationToken cancellationToken = default)
    {
        _dbContext.DoctorAvailabilities.Update(availability);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DoctorAvailability availability, CancellationToken cancellationToken = default)
    {
        _dbContext.DoctorAvailabilities.Remove(availability);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


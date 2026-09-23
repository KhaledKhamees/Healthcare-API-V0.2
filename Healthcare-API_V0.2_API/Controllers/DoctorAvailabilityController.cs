using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Availability;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/doctor-clinics/{doctorClinicId:guid}/availability")]
public class DoctorAvailabilityController : ControllerBase
{
    private readonly IDoctorAvailabilityService _availabilityService;

    public DoctorAvailabilityController(IDoctorAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDoctorClinic(Guid doctorClinicId, CancellationToken cancellationToken)
    {
        try
        {
            var availabilities = await _availabilityService.GetByDoctorClinicAsync(doctorClinicId, cancellationToken);
            return Ok(availabilities.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid doctorClinicId, [FromBody] CreateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var availability = await _availabilityService.CreateAsync(doctorClinicId, request.DayOfWeek, request.StartTime, request.EndTime, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(availability));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{availabilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTimeSlot(Guid doctorClinicId, Guid availabilityId, [FromBody] UpdateAvailabilityTimeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _availabilityService.UpdateTimeSlotAsync(doctorClinicId, availabilityId, request.StartTime, request.EndTime, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{availabilityId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken)
    {
        try
        {
            await _availabilityService.ActivateAsync(doctorClinicId, availabilityId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{availabilityId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken)
    {
        try
        {
            await _availabilityService.DeactivateAsync(doctorClinicId, availabilityId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{availabilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid doctorClinicId, Guid availabilityId, CancellationToken cancellationToken)
    {
        try
        {
            await _availabilityService.DeleteAsync(doctorClinicId, availabilityId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static AvailabilityResponse ToResponse(DoctorAvailability availability)
    {
        return new AvailabilityResponse
        {
            AvailabilityId = availability.AvailabilityId,
            DoctorClinicId = availability.DoctorClinicId,
            DayOfWeek = availability.DayOfWeek,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            IsActive = availability.IsActive,
            CreatedAt = availability.CreatedAt,
            UpdatedAt = availability.UpdatedAt
        };
    }
}

using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.DoctorClinics;
using Healthcare_API_V0._2_Application.DTOs.Doctors;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IDoctorClinicService _doctorClinicService;

    public DoctorsController(IDoctorService doctorService, IDoctorClinicService doctorClinicService)
    {
        _doctorService = doctorService;
        _doctorClinicService = doctorClinicService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
            if (doctor is null)
            {
                return NotFound(new { message = $"Doctor with ID {id} was not found." });
            }

            return Ok(ToResponse(doctor));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var applicationUserId))
        {
            return BadRequest(new { message = "Authenticated user identifier is missing." });
        }

        var doctor = await _doctorService.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);
        if (doctor is null)
        {
            return NotFound(new { message = "No doctor profile was found for the authenticated user." });
        }

        return Ok(ToResponse(doctor));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DoctorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySpecialization([FromQuery] Guid specializationId, CancellationToken cancellationToken)
    {
        try
        {
            var doctors = await _doctorService.GetBySpecializationAsync(specializationId, cancellationToken);
            return Ok(doctors.Select(ToResponse).ToList());
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
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _doctorService.UpdateDoctorAsync(id, request.FirstName, request.LastName, request.LicenseNumber, request.Bio, cancellationToken);
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
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeDoctorStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _doctorService.ChangeStatusAsync(id, request.Status, cancellationToken);
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _doctorService.DeleteAsync(id, cancellationToken);
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
    [HttpPost("{doctorId:guid}/clinics")]
    [ProducesResponseType(typeof(DoctorClinicResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignClinic(Guid doctorId, [FromBody] AssignDoctorClinicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var doctorClinic = await _doctorClinicService.CreateAsync(doctorId, request.ClinicId, request.ConsultationFee, request.AppointmentDurationMinutes, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(doctorClinic));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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

    [HttpGet("{doctorId:guid}/clinics/{clinicId:guid}")]
    [ProducesResponseType(typeof(DoctorClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClinicAssociation(Guid doctorId, Guid clinicId, CancellationToken cancellationToken)
    {
        try
        {
            var doctorClinic = await _doctorClinicService.GetByDoctorAndClinicAsync(doctorId, clinicId, cancellationToken);
            if (doctorClinic is null)
            {
                return NotFound(new { message = "No association was found for this doctor and clinic." });
            }

            return Ok(ToResponse(doctorClinic));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static DoctorResponse ToResponse(Doctor doctor)
    {
        return new DoctorResponse
        {
            DoctorId = doctor.DoctorId,
            ApplicationUserId = doctor.ApplicationUserId,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            LicenseNumber = doctor.LicenseNumber,
            Bio = doctor.Bio,
            Status = doctor.Status,
            SpecializationId = doctor.SpecializationId,
            CreatedAt = doctor.CreatedAt,
            UpdatedAt = doctor.UpdatedAt
        };
    }

    private static DoctorClinicResponse ToResponse(DoctorClinic doctorClinic)
    {
        return new DoctorClinicResponse
        {
            DoctorClinicId = doctorClinic.DoctorClinicId,
            DoctorId = doctorClinic.DoctorId,
            ClinicId = doctorClinic.ClinicId,
            ConsultationFee = doctorClinic.ConsultationFee,
            AppointmentDurationMinutes = doctorClinic.AppointmentDurationMinutes,
            CreatedAt = doctorClinic.CreatedAt,
            UpdatedAt = doctorClinic.UpdatedAt
        };
    }
}

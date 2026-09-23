using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Appointments;
using Healthcare_API_V0._2_Application.DTOs.Patients;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;

    public PatientsController(IPatientService patientService, IAppointmentService appointmentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var patient = await _patientService.GetByIdAsync(id, cancellationToken);
            if (patient is null)
            {
                return NotFound(new { message = $"Patient with ID {id} was not found." });
            }

            return Ok(ToResponse(patient));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var applicationUserId))
        {
            return BadRequest(new { message = "Authenticated user identifier is missing." });
        }

        var patient = await _patientService.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);
        if (patient is null)
        {
            return NotFound(new { message = "No patient profile was found for the authenticated user." });
        }

        return Ok(ToResponse(patient));
    }

    [HttpGet("{id:guid}/appointments")]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAppointments(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var appointments = await _appointmentService.GetByPatientAsync(id, cancellationToken);
            return Ok(appointments.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _patientService.DeleteAsync(id, cancellationToken);
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

    private static PatientResponse ToResponse(Patient patient)
    {
        return new PatientResponse
        {
            PatientId = patient.PatientId,
            ApplicationUserId = patient.ApplicationUserId,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }

    private static AppointmentResponse ToResponse(Appointment appointment)
    {
        return new AppointmentResponse
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorClinicId = appointment.DoctorClinicId,
            StartTime = appointment.StartTime,
            ReasonForVisit = appointment.ReasonForVisit,
            Status = appointment.Status,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }
}

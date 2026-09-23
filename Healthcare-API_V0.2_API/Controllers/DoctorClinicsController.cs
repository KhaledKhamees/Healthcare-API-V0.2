using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Appointments;
using Healthcare_API_V0._2_Application.DTOs.DoctorClinics;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/doctor-clinics")]
public class DoctorClinicsController : ControllerBase
{
    private readonly IDoctorClinicService _doctorClinicService;
    private readonly IAppointmentService _appointmentService;

    public DoctorClinicsController(IDoctorClinicService doctorClinicService, IAppointmentService appointmentService)
    {
        _doctorClinicService = doctorClinicService;
        _appointmentService = appointmentService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var doctorClinic = await _doctorClinicService.GetByIdAsync(id, cancellationToken);
            if (doctorClinic is null)
            {
                return NotFound(new { message = $"Doctor-clinic with ID {id} was not found." });
            }

            return Ok(ToResponse(doctorClinic));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/appointments")]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAppointments(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var appointments = await _appointmentService.GetByDoctorClinicAsync(id, cancellationToken);
            return Ok(appointments.Select(ToAppointmentResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id:guid}/fee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFee(Guid id, [FromBody] UpdateConsultationFeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _doctorClinicService.UpdateConsultationFeeAsync(id, request.ConsultationFee, cancellationToken);
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
    [HttpPut("{id:guid}/duration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDuration(Guid id, [FromBody] UpdateAppointmentDurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _doctorClinicService.UpdateAppointmentDurationAsync(id, request.AppointmentDurationMinutes, cancellationToken);
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
            await _doctorClinicService.DeleteAsync(id, cancellationToken);
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

    private static AppointmentResponse ToAppointmentResponse(Appointment appointment)
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

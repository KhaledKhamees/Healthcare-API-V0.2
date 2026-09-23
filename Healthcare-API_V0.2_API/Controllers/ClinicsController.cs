using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Clinics;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/clinics")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var clinic = await _clinicService.GetByIdAsync(id, cancellationToken);
            if (clinic is null)
            {
                return NotFound(new { message = $"Clinic with ID {id} was not found." });
            }

            return Ok(ToResponse(clinic));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-city")]
    [ProducesResponseType(typeof(List<ClinicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCity([FromQuery] string city, CancellationToken cancellationToken)
    {
        try
        {
            var clinics = await _clinicService.GetByCityAsync(city, cancellationToken);
            return Ok(clinics.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ClinicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var clinic = await _clinicService.CreateAsync(request.Name, request.Address, request.City, request.PhoneNumber, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(clinic));
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
    public async Task<IActionResult> Update(Guid id, [FromBody] ClinicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _clinicService.UpdateClinicAsync(id, request.Name, request.Address, request.City, request.PhoneNumber, cancellationToken);
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
            await _clinicService.DeleteAsync(id, cancellationToken);
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

    private static ClinicResponse ToResponse(Clinic clinic)
    {
        return new ClinicResponse
        {
            ClinicId = clinic.ClinicId,
            Name = clinic.Name,
            Address = clinic.Address,
            City = clinic.City,
            PhoneNumber = clinic.PhoneNumber,
            CreatedAt = clinic.CreatedAt,
            UpdatedAt = clinic.UpdatedAt
        };
    }
}

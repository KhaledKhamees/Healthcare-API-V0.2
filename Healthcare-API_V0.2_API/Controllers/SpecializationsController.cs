using Healthcare_API.Models;
using Healthcare_API_V0._2_Application.DTOs.Specializations;
using Healthcare_API_V0._2_Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Healthcare_API_V0._2_API.Controllers;

[ApiController]
[Route("api/specializations")]
public class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationsController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SpecializationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var specialization = await _specializationService.GetByIdAsync(id, cancellationToken);
            if (specialization is null)
            {
                return NotFound(new { message = $"Specialization with ID {id} was not found." });
            }

            return Ok(ToResponse(specialization));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-name")]
    [ProducesResponseType(typeof(SpecializationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        try
        {
            var specialization = await _specializationService.GetByNameAsync(name, cancellationToken);
            if (specialization is null)
            {
                return NotFound(new { message = $"Specialization with name '{name}' was not found." });
            }

            return Ok(ToResponse(specialization));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(SpecializationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SpecializationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var specialization = await _specializationService.CreateAsync(request.Name, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(specialization));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SpecializationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _specializationService.UpdateAsync(id, request.Name, cancellationToken);
            return NoContent();
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

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _specializationService.DeleteAsync(id, cancellationToken);
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

    private static SpecializationResponse ToResponse(Specialization specialization)
    {
        return new SpecializationResponse
        {
            SpecializationId = specialization.SpecializationId,
            Name = specialization.Name,
            CreatedAt = specialization.CreatedAt,
            UpdatedAt = specialization.UpdatedAt
        };
    }
}

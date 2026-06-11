using Microsoft.AspNetCore.Mvc;
using HospitalApi.DTOs;
using HospitalApi.Exceptions;
using HospitalApi.Services;

namespace HospitalApi.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(IPatientService service) : ControllerBase
{
    // GET /api/patients
    // GET /api/patients?search=an
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        return Ok(await service.GetAllAsync(search, ct));
    }

    // POST /api/patients/{pesel}/bedassignments
    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed(
        [FromRoute] string pesel,
        [FromBody] CreateBedAssignmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await service.AssignBedAsync(pesel, request, ct);
            return Created(string.Empty, result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
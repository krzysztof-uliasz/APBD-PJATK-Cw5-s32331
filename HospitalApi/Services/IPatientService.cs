using HospitalApi.DTOs;

namespace HospitalApi.Services;

public interface IPatientService
{
    Task<IEnumerable<PatientResponse>> GetAllAsync(string? search, CancellationToken ct);
    Task<BedAssignmentDto> AssignBedAsync(string pesel, CreateBedAssignmentRequest request, CancellationToken ct);
}
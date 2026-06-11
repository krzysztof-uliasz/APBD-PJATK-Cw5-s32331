using Microsoft.EntityFrameworkCore;
using HospitalApi.DTOs;
using HospitalApi.Exceptions;
using HospitalApi.Infrastructure;
using HospitalApi.Models;

namespace HospitalApi.Services;

public class PatientService(HospitalContext ctx) : IPatientService
{

    public async Task<IEnumerable<PatientResponse>> GetAllAsync(string? search, CancellationToken ct)
    {
        var query = ctx.Patients
            .Include(p => p.Admissions).ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed).ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed).ThenInclude(b => b.Room).ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, $"%{search}%") ||
                EF.Functions.Like(p.LastName,  $"%{search}%"));

        var patients = await query.ToListAsync(ct);

        return patients.Select(p => new PatientResponse(
            p.Pesel,
            p.FirstName,
            p.LastName,
            p.Age,
            p.Sex ? "Male" : "Female",
            p.Admissions.Select(a => new AdmissionDto(
                a.Id,
                a.AdmissionDate,
                a.DischargeDate,
                new WardDto(a.Ward.Id, a.Ward.Name, a.Ward.Description)
            )),
            p.BedAssignments.Select(ba => new BedAssignmentDto(
                ba.Id,
                ba.From,
                ba.To,
                new BedDto(
                    ba.Bed.Id,
                    new BedTypeDto(ba.Bed.BedType.Id, ba.Bed.BedType.Name, ba.Bed.BedType.Description),
                    new RoomDto(
                        ba.Bed.Room.Id,
                        ba.Bed.Room.HasTv,
                        new WardDto(ba.Bed.Room.Ward.Id, ba.Bed.Room.Ward.Name, ba.Bed.Room.Ward.Description)
                    )
                )
            ))
        ));
    }


    public async Task<BedAssignmentDto> AssignBedAsync(
        string pesel,
        CreateBedAssignmentRequest request,
        CancellationToken ct)
    {
        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.Pesel == pesel, ct)
            ?? throw new NotFoundException($"Patient with PESEL '{pesel}' not found.");

        var ward = await ctx.Wards.FirstOrDefaultAsync(w => w.Name == request.Ward, ct)
            ?? throw new NotFoundException($"Ward '{request.Ward}' not found.");

        var bedType = await ctx.BedTypes.FirstOrDefaultAsync(bt => bt.Name == request.BedType, ct)
            ?? throw new NotFoundException($"Bed type '{request.BedType}' not found.");
        
        var requestTo = request.To ?? DateTime.MaxValue;

        var freeBed = await ctx.Beds
            .Include(b => b.Room).ThenInclude(r => r.Ward)
            .Include(b => b.BedType)
            .Where(b =>
                b.BedTypeId == bedType.Id &&
                b.Room.WardId == ward.Id &&
                !b.BedAssignments.Any(ba =>
                    ba.From < requestTo &&
                    (ba.To == null || ba.To > request.From)
                )
            )
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(
                $"No available '{request.BedType}' bed found in ward '{request.Ward}' " +
                $"for the period {request.From:yyyy-MM-dd} – {request.To?.ToString("yyyy-MM-dd") ?? "open-ended"}.");

        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId        = freeBed.Id,
            From         = request.From,
            To           = request.To
        };

        ctx.BedAssignments.Add(assignment);
        await ctx.SaveChangesAsync(ct);

        return new BedAssignmentDto(
            assignment.Id,
            assignment.From,
            assignment.To,
            new BedDto(
                freeBed.Id,
                new BedTypeDto(freeBed.BedType.Id, freeBed.BedType.Name, freeBed.BedType.Description),
                new RoomDto(
                    freeBed.Room.Id,
                    freeBed.Room.HasTv,
                    new WardDto(freeBed.Room.Ward.Id, freeBed.Room.Ward.Name, freeBed.Room.Ward.Description)
                )
            )
        );
    }
}
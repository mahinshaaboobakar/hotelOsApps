using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;
using HotelOS.Jobs.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Jobs.Application.Jobs;

/// <summary>
/// The job number — S1 D3: <c>MRN-ENG-142</c>, property code upper, the
/// category's root department, one counter per property so the number is
/// common across departments. The property code is Master Data's, read once
/// and cached on the sequence row.
/// </summary>
public class JobNumbering(JobsDbContext db, IPropertyDirectory directory)
{
    /// <summary>Take the next number for the property, inside the caller's transaction.</summary>
    public async Task<string> NextAsync(
        Guid propertyId, string departmentCode, CancellationToken cancellationToken)
    {
        var sequence = await db.Sequences
            .FirstOrDefaultAsync(s => s.PropertyId == propertyId, cancellationToken);

        if (sequence is null)
        {
            var code = await directory.FindPropertyCodeAsync(propertyId, cancellationToken)
                ?? throw new InvalidRequestException(
                    "the property has no code in Master Data; a job number needs one");

            sequence = new PropertyJobSequence
            {
                PropertyId = propertyId,
                PropertyCode = code.Trim().ToUpperInvariant(),
                Next = 1,
            };
            db.Sequences.Add(sequence);
        }

        var number = sequence.Next;
        sequence.Next += 1;

        return $"{sequence.PropertyCode}-{departmentCode.ToUpperInvariant()}-{number}";
    }
}

using System.Security.Cryptography;
using System.Text;
using HotelOS.GuestOps.Application.Abstractions;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Domain;

namespace HotelOS.GuestOps.Infrastructure.Platform;

/// <summary>
/// Contact details, encrypted, with the blind index beside them.
/// </summary>
/// <remarks>
/// <para>
/// The mechanism is the platform's, designed before this application existed:
/// <c>aes_gcm_encrypt(field_key, e164(phone))</c> stored beside
/// <c>hmac_sha256(index_key, e164(phone))</c>. Exact match at full index speed,
/// no partial or prefix search — the accepted cost, and the reason the WhatsApp
/// flow resolves on a complete number rather than a fragment.
/// </para>
/// <para>
/// <b>Normalisation is here, once.</b> The index is an HMAC of the
/// <i>normalised</i> value, so a caller that normalised differently would write
/// a row nothing could ever find — the failure would be a guest who exists and
/// cannot be looked up, which nobody notices until someone rings.
/// </para>
/// <para>
/// <b>The keys are not this application's, and that is now RULED</b> — ADR 0366
/// §1 and ADR 0367 §1. Both are platform-generated, sealed persistent
/// cryptographic state, handed to this process at start; neither may be
/// regenerated because a package lifecycle ran again. This implementation takes
/// them as bytes so the composition root decides where they come from, which is
/// the shape the ruling then required.
/// </para>
/// <para>
/// <b>This pointed at "round 51's" answer, and the real one is ADR 0366/0367.</b>
/// The clause <i>field key or index key</i> was already right about the
/// POPULATION while ADR 0366 named only <c>Pii:FieldKey</c> — ADR 0367 §1 and §3
/// correct that, and §2 closes the population at exactly these two: no
/// <c>Pii:*</c> wildcard.
/// </para>
/// <para>
/// <b>The two keys fail differently and share one lifecycle</b> — ADR 0367 §1.
/// A replaced field key makes protected values unreadable; a replaced index key
/// makes blind indexes stop matching. Different symptom, identical invariant,
/// and only one of the two is visible by looking at a row.
/// </para>
/// </remarks>
public sealed class ContactProtector(byte[] fieldKey, byte[] indexKey) : IContactProtector
{
    public IReadOnlyList<ContactPoint> Protect(NewGuest guest)
    {
        var points = new List<ContactPoint>();

        if (Normalise(guest.Phone) is { } phone)
        {
            points.Add(Build(ContactKind.Phone, phone, guest.IsPrimary));
        }

        if (Normalise(guest.Email) is { } email)
        {
            points.Add(Build(ContactKind.Email, email, guest.IsPrimary));
        }

        // Empty is a valid answer, and deliberately not an error: a stay with no
        // contact detail is a real stay, and the system this replaces dropped
        // one silently rather than record that (R25).
        return points;
    }

    /// <summary>The form both the ciphertext and the index are taken over.</summary>
    /// <remarks>
    /// Trimmed and lower-cased. <b>Not</b> reformatted into E.164 here: doing
    /// that properly needs a region, and guessing one would make two spellings
    /// of one number that never match — the same class of silent mismatch this
    /// normalisation exists to prevent. A phone arriving in a national format
    /// is stored as it was given and found by the same string.
    /// </remarks>
    private static string? Normalise(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private ContactPoint Build(ContactKind kind, string value, bool? isPrimary)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);

        // A fresh nonce per value, carried with the ciphertext. Reusing one
        // across two contacts would leak that they are equal, which for a phone
        // number is most of what an attacker wants to know.
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using (var aes = new AesGcm(fieldKey, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, cipher, tag);
        }

        using var hmac = new HMACSHA256(indexKey);

        return new ContactPoint
        {
            Kind = kind,
            ValueCipher = [.. nonce, .. tag, .. cipher],
            ValueIndex = hmac.ComputeHash(plaintext),
            IsPrimary = isPrimary,
            Origin = RecordOrigin.Staff,
        };
    }
}

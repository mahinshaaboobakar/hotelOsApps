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
/// <b>The keys are not this application's.</b> <c>HUB-Q1</c>'s sibling question:
/// an installed package has no access to the platform's field key or index key,
/// because nothing hands one to a package at install. This implementation takes
/// them as bytes so the composition root decides where they come from, and the
/// answer for a <c>.hopkg</c> is round 51's.
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

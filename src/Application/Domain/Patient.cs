using System.Text.RegularExpressions;

using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain;

public class Patient : AuditableEntity
{
    public Patient(string fullName, string email, string phone)
    {
        UpdateFullName(fullName);
        UpdateEmail(email);
        UpdatePhone(phone);
    }

    private Patient()
    {
        // Private parameterless constructor for EF Core
    }

    public Guid Id { get; internal set; }
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;

    public void UpdateFullName(string newFullName)
    {
        if (string.IsNullOrWhiteSpace(newFullName))
        {
            throw new ArgumentException("Full name cannot be empty", nameof(newFullName));
        }

        if (newFullName.Length > 100)
        {
            throw new ArgumentException("Full name cannot exceed 100 characters", nameof(newFullName));
        }

        FullName = newFullName.Trim();
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
        {
            throw new ArgumentException("Email cannot be empty", nameof(newEmail));
        }

        if (!IsValidEmail(newEmail))
        {
            throw new ArgumentException("Invalid email format", nameof(newEmail));
        }

        Email = newEmail.Trim().ToLowerInvariant();
    }

    public void UpdatePhone(string newPhone)
    {
        if (string.IsNullOrWhiteSpace(newPhone))
        {
            throw new ArgumentException("Phone cannot be empty", nameof(newPhone));
        }

        var cleanPhone = newPhone.Trim();
        if (!IsValidPhone(cleanPhone))
        {
            throw new ArgumentException("Invalid phone format", nameof(newPhone));
        }

        Phone = cleanPhone;
    }

    private static bool IsValidEmail(string email)
    {
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsValidPhone(string phone)
    {
        // Allow various phone formats: +1-555-123-4567, (555) 123-4567, 555.123.4567, etc.
        const string phonePattern = @"^[\+]?[1-9]?[\d\s\-\(\)\.]{10,15}$";
        return Regex.IsMatch(phone, phonePattern);
    }
}
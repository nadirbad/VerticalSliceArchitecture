using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain;

public class Doctor : AuditableEntity
{
    public Doctor(string fullName, string specialty)
    {
        UpdateFullName(fullName);
        UpdateSpecialty(specialty);
    }

    private Doctor()
    {
        // Private parameterless constructor for EF Core
    }

    public Guid Id { get; internal set; }
    public string FullName { get; private set; } = null!;
    public string Specialty { get; private set; } = null!;

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

    public void UpdateSpecialty(string newSpecialty)
    {
        if (string.IsNullOrWhiteSpace(newSpecialty))
        {
            throw new ArgumentException("Specialty cannot be empty", nameof(newSpecialty));
        }

        if (newSpecialty.Length > 50)
        {
            throw new ArgumentException("Specialty cannot exceed 50 characters", nameof(newSpecialty));
        }

        Specialty = newSpecialty.Trim();
    }
}
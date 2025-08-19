using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class Doctor : AuditableEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Specialty { get; set; } = null!;
}

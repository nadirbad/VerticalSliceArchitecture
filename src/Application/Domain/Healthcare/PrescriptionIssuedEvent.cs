using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Healthcare;

public class PrescriptionIssuedEvent(
    Guid prescriptionId,
    Guid patientId,
    Guid doctorId,
    string medicationName,
    string dosage,
    DateTime issuedDateUtc,
    DateTime expirationDateUtc) : DomainEvent
{
    public Guid PrescriptionId { get; } = prescriptionId;
    public Guid PatientId { get; } = patientId;
    public Guid DoctorId { get; } = doctorId;
    public string MedicationName { get; } = medicationName;
    public string Dosage { get; } = dosage;
    public DateTime IssuedDateUtc { get; } = issuedDateUtc;
    public DateTime ExpirationDateUtc { get; } = expirationDateUtc;
}
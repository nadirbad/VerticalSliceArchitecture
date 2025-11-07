# Database Schema

This is the database schema implementation for the spec detailed in [@.agent-os/specs/2025-10-01-prescription-issuance/spec.md](../.agent-os/specs/2025-10-01-prescription-issuance/spec.md)

## Entity Framework Configuration

**File:** `src/Application/Features/Healthcare/Entities/PrescriptionConfiguration.cs`

```csharp
internal class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MedicationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Dosage)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Directions)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.NumberOfRefills)
            .IsRequired();

        builder.Property(p => p.RemainingRefills)
            .IsRequired();

        builder.Property(p => p.IssuedDateUtc)
            .IsRequired();

        builder.Property(p => p.ExpirationDateUtc)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        // Navigation properties
        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Doctor)
            .WithMany()
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.DoctorId);
        builder.HasIndex(p => p.ExpirationDateUtc);
    }
}
```

## Migration

**Command to generate migration:**
```bash
dotnet ef migrations add "AddPrescriptions" --project src/Application --startup-project src/Api --output-dir Infrastructure/Persistence/Migrations
```

**Expected Migration (Up):**
```csharp
public partial class AddPrescriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Prescriptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PatientId = table.Column<int>(type: "int", nullable: false),
                DoctorId = table.Column<int>(type: "int", nullable: false),
                MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Dosage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Directions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                NumberOfRefills = table.Column<int>(type: "int", nullable: false),
                RemainingRefills = table.Column<int>(type: "int", nullable: false),
                IssuedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpirationDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prescriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Prescriptions_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Prescriptions_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_DoctorId",
            table: "Prescriptions",
            column: "DoctorId");

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_ExpirationDateUtc",
            table: "Prescriptions",
            column: "ExpirationDateUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_PatientId",
            table: "Prescriptions",
            column: "PatientId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Prescriptions");
    }
}
```

## DbContext Registration

**File:** `src/Application/Infrastructure/Persistence/ApplicationDbContext.cs`

Add DbSet:
```csharp
public DbSet<Prescription> Prescriptions => Set<Prescription>();
```

Ensure configuration is applied:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    base.OnModelCreating(modelBuilder);
}
```

## Schema Rationale

### Foreign Keys with Restrict Delete Behavior
- **Reason:** Prescriptions must maintain referential integrity. If a Patient or Doctor is deleted, prescriptions should remain for audit/compliance purposes.
- **Alternative:** Consider soft deletes for Patient and Doctor entities if not already implemented.

### String Column Max Lengths
- **MedicationName (200):** Accommodates long medication names including brand and generic names.
- **Dosage (50):** Sufficient for dosage like "500mg twice daily" or "2.5ml every 6 hours."
- **Directions (500):** Allows detailed instructions for complex medication regimens.
- **Status (50):** Enum stored as string for readability in database queries.

### Indexes
- **PatientId:** Frequently queried to show patient's prescription list.
- **DoctorId:** Used to query prescriptions issued by specific doctor.
- **ExpirationDateUtc:** Enables efficient queries for expired prescriptions cleanup or reports.

### DateTime2 Type
- **Reason:** datetime2 provides better precision and range than datetime in SQL Server.
- **UTC:** All dates stored in UTC to avoid timezone issues.

### Status as Enum Converted to String
- **Reason:** Provides type safety in C# while maintaining readability in database.
- **Alternative:** Could use int with lookup table, but string is simpler for this domain.

### No Soft Delete
- **Reason:** Prescriptions are immutable audit records. Once issued, they should never be deleted.
- **Consideration:** If soft delete is needed later, add `IsDeleted` and `DeletedDateUtc` columns.

## Data Seeding (Optional)

**File:** `src/Application/Infrastructure/Persistence/ApplicationDbContextInitializer.cs`

Add sample prescriptions for testing:
```csharp
if (!context.Prescriptions.Any())
{
    var prescriptions = new[]
    {
        Prescription.Issue(
            patientId: 1,
            doctorId: 1,
            medicationName: "Amoxicillin",
            dosage: "500mg",
            directions: "Take one capsule three times daily with food for 10 days",
            quantity: 30,
            numberOfRefills: 2,
            durationInDays: 90
        ),
        Prescription.Issue(
            patientId: 2,
            doctorId: 1,
            medicationName: "Lisinopril",
            dosage: "10mg",
            directions: "Take one tablet once daily in the morning",
            quantity: 90,
            numberOfRefills: 5,
            durationInDays: 180
        )
    };

    context.Prescriptions.AddRange(prescriptions);
    await context.SaveChangesAsync();
}
```

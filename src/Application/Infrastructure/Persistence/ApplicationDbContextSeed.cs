using VerticalSliceArchitecture.Application.Domain.Healthcare;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Domain.ValueObjects;

namespace VerticalSliceArchitecture.Application.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedSampleDataAsync(ApplicationDbContext context)
    {
        // Seed, if necessary
        if (!context.TodoLists.Any())
        {
            context.TodoLists.Add(new TodoList
            {
                Title = "Shopping",
                Colour = Colour.Blue,
                Items =
                    {
                        new TodoItem { Title = "Apples", Done = true },
                        new TodoItem { Title = "Milk", Done = true },
                        new TodoItem { Title = "Bread", Done = true },
                        new TodoItem { Title = "Toilet paper" },
                        new TodoItem { Title = "Pasta" },
                        new TodoItem { Title = "Tissues" },
                        new TodoItem { Title = "Tuna" },
                        new TodoItem { Title = "Water" },
                    },
            });

            await context.SaveChangesAsync();
        }

        // Seed Healthcare data
        if (!context.Patients.Any())
        {
            context.Patients.AddRange(
                new Patient("John Smith", "john.smith@example.com", "+1-555-0101") { Id = new Guid("11111111-1111-1111-1111-111111111111") },
                new Patient("Jane Doe", "jane.doe@example.com", "+1-555-0102") { Id = new Guid("22222222-2222-2222-2222-222222222222") },
                new Patient("Bob Johnson", "bob.johnson@example.com", "+1-555-0103") { Id = new Guid("33333333-3333-3333-3333-333333333333") });

            context.Doctors.AddRange(
                new Doctor("Dr. Sarah Wilson", "Family Medicine") { Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                new Doctor("Dr. Michael Chen", "Cardiology") { Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                new Doctor("Dr. Emily Rodriguez", "Pediatrics") { Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") });

            await context.SaveChangesAsync();
        }

        // Seed Prescription data
        if (!context.Prescriptions.Any())
        {
            var patientId1 = new Guid("11111111-1111-1111-1111-111111111111");
            var patientId2 = new Guid("22222222-2222-2222-2222-222222222222");
            var patientId3 = new Guid("33333333-3333-3333-3333-333333333333");

            var doctorId1 = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var doctorId2 = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var doctorId3 = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");

            // Create prescriptions using the Issue factory method
            var prescription1 = Prescription.Issue(
                patientId1,
                doctorId1,
                "Amoxicillin",
                "500mg",
                "Take one capsule three times daily with food",
                30,
                2,
                90);
            prescription1.Id = new Guid("11111111-2222-3333-4444-555555555555");

            var prescription2 = Prescription.Issue(
                patientId2,
                doctorId2,
                "Lisinopril",
                "10mg",
                "Take one tablet daily in the morning",
                90,
                3,
                365);
            prescription2.Id = new Guid("22222222-3333-4444-5555-666666666666");

            var prescription3 = Prescription.Issue(
                patientId3,
                doctorId3,
                "Albuterol Inhaler",
                "90mcg/actuation",
                "Inhale 2 puffs every 4-6 hours as needed for breathing",
                1,
                5,
                180);
            prescription3.Id = new Guid("33333333-4444-5555-6666-777777777777");

            var prescription4 = Prescription.Issue(
                patientId1,
                doctorId2,
                "Metformin",
                "1000mg",
                "Take one tablet twice daily with meals",
                60,
                6,
                180);
            prescription4.Id = new Guid("44444444-5555-6666-7777-888888888888");

            // Add a prescription with no refills
            var prescription5 = Prescription.Issue(
                patientId2,
                doctorId1,
                "Prednisone",
                "20mg",
                "Take 2 tablets daily for 5 days, then 1 tablet daily for 5 days",
                15,
                0,
                10);
            prescription5.Id = new Guid("55555555-6666-7777-8888-999999999999");

            context.Prescriptions.AddRange(prescription1, prescription2, prescription3, prescription4, prescription5);

            await context.SaveChangesAsync();
        }
    }
}
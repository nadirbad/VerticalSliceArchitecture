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
    }
}
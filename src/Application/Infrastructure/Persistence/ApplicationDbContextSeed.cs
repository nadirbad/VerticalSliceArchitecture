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
                new Patient { FullName = "John Smith", Email = "john.smith@example.com", Phone = "+1-555-0101" },
                new Patient { FullName = "Jane Doe", Email = "jane.doe@example.com", Phone = "+1-555-0102" },
                new Patient { FullName = "Bob Johnson", Email = "bob.johnson@example.com", Phone = "+1-555-0103" });

            context.Doctors.AddRange(
                new Doctor { FullName = "Dr. Sarah Wilson", Specialty = "Family Medicine" },
                new Doctor { FullName = "Dr. Michael Chen", Specialty = "Cardiology" },
                new Doctor { FullName = "Dr. Emily Rodriguez", Specialty = "Pediatrics" });

            await context.SaveChangesAsync();
        }
    }
}
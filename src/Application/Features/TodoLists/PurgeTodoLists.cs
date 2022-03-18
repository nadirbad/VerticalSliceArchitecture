using MediatR;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class PurgeTodoLists : IRequest
{
}

internal class PurgeTodoListsCommandHandler : IRequestHandler<PurgeTodoLists>
{
    private readonly ApplicationDbContext _context;

    public PurgeTodoListsCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(PurgeTodoLists request, CancellationToken cancellationToken)
    {
        _context.TodoLists.RemoveRange(_context.TodoLists);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

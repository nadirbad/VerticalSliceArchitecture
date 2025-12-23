using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Common.Security;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoLists;

public class GetMyTodoListsController : ApiControllerBase
{
    [HttpGet("/api/my-todo-lists")]
    public async Task<IActionResult> GetMyTodoLists([FromQuery] GetMyTodoListsQuery query)
    {
        var result = await Mediator.Send(query);

        return result.Match(
            lists => Ok(lists),
            Problem);
    }
}

/// <summary>
/// Example feature that demonstrates user identification.
/// This query returns only todo lists created by the current authenticated user.
/// </summary>
[Authorize] // This attribute ensures only authenticated users can access this endpoint
public record GetMyTodoListsQuery() : IRequest<ErrorOr<List<TodoListBriefDto>>>;

public record TodoListBriefDto(int Id, string Title, int ItemCount, DateTime Created);

internal sealed class GetMyTodoListsQueryValidator : AbstractValidator<GetMyTodoListsQuery>
{
    public GetMyTodoListsQueryValidator()
    {
        // No validation rules needed for this simple query
    }
}

internal sealed class GetMyTodoListsQueryHandler : IRequestHandler<GetMyTodoListsQuery, ErrorOr<List<TodoListBriefDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTodoListsQueryHandler(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ErrorOr<List<TodoListBriefDto>>> Handle(GetMyTodoListsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        // Filter todo lists by the current authenticated user
        // This demonstrates how user identification works in practice
        var todoLists = await _context.TodoLists
            .AsNoTracking()
            .Where(tl => tl.CreatedBy == currentUserId) // Only return lists created by current user
            .Select(tl => new TodoListBriefDto(
                tl.Id,
                tl.Title!,
                tl.Items.Count,
                tl.Created))
            .ToListAsync(cancellationToken);

        return todoLists;
    }
}
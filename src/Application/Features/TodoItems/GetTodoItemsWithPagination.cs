using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Mappings;
using VerticalSliceArchitecture.Application.Common.Models;
using VerticalSliceArchitecture.Application.Domain.Todos;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.TodoItems;

public class GetTodoItemsWithPaginationController : ApiControllerBase
{
    [HttpGet("/api/todo-items")]
    public async Task<IActionResult> GetTodoItemsWithPagination([FromQuery] GetTodoItemsWithPaginationQuery query)
    {
        var result = await Mediator.Send(query);

        return result.Match(
            Ok,
            Problem);
    }
}

public record TodoItemBriefResponse(int Id, int ListId, string? Title, bool Done);

public record GetTodoItemsWithPaginationQuery(int ListId, int PageNumber = 1, int PageSize = 10) : IRequest<ErrorOr<PaginatedList<TodoItemBriefResponse>>>;

internal sealed class GetTodoItemsWithPaginationQueryValidator : AbstractValidator<GetTodoItemsWithPaginationQuery>
{
    public GetTodoItemsWithPaginationQueryValidator()
    {
        RuleFor(x => x.ListId)
            .NotEmpty().WithMessage("ListId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}

internal sealed class GetTodoItemsWithPaginationQueryHandler(ApplicationDbContext context) : IRequestHandler<GetTodoItemsWithPaginationQuery, ErrorOr<PaginatedList<TodoItemBriefResponse>>>
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ErrorOr<PaginatedList<TodoItemBriefResponse>>> Handle(GetTodoItemsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var paginatedList = await _context.TodoItems
            .Where(item => item.ListId == request.ListId)
            .OrderBy(item => item.Title)
            .Select(item => ToDto(item))
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return paginatedList;
    }

    private static TodoItemBriefResponse ToDto(TodoItem todoItem) =>
        new(todoItem.Id, todoItem.ListId, todoItem.Title, todoItem.Done);
}
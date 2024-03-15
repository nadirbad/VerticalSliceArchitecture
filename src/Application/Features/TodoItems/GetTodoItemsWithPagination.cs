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
    public Task<PaginatedList<TodoItemBriefResponse>> GetTodoItemsWithPagination([FromQuery] GetTodoItemsWithPaginationQuery query)
    {
        return Mediator.Send(query);
    }
}

public record TodoItemBriefResponse(int Id, int ListId, string? Title, bool Done);

public record GetTodoItemsWithPaginationQuery(int ListId, int PageNumber = 1, int PageSize = 10) : IRequest<PaginatedList<TodoItemBriefResponse>>;

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

internal sealed class GetTodoItemsWithPaginationQueryHandler(ApplicationDbContext context) : IRequestHandler<GetTodoItemsWithPaginationQuery, PaginatedList<TodoItemBriefResponse>>
{
    private readonly ApplicationDbContext _context = context;

    public Task<PaginatedList<TodoItemBriefResponse>> Handle(GetTodoItemsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        return _context.TodoItems
            .Where(item => item.ListId == request.ListId)
            .OrderBy(item => item.Title)
            .Select(item => ToDto(item))
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }

    private static TodoItemBriefResponse ToDto(TodoItem todoItem) =>
        new(todoItem.Id, todoItem.ListId, todoItem.Title, todoItem.Done);
}
using System.Text.Json.Serialization;

namespace SPSS.App.Features.History.Commands;

[Authorize(Roles = CreateMonitHistory)]
public sealed record AddMonitHistoryCommand(List<MonitHistoryDto> Data) : IRequest<Response<int>>;
public sealed class MonitHistoryDto
{
    [JsonPropertyName("institution_name")]
    public string InstitutionName { get; set; } = default!;

    [JsonPropertyName("institution_bic")]
    public string InstitutionBic { get; set; } = default!;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("http_status")]
    public int HttpStatus { get; set; }

    [JsonPropertyName("response")]
    public object? Response { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_type")]
    public string? ErrorType { get; set; }

    [JsonPropertyName("checked_at")]
    public DateTimeOffset CheckedAt { get; set; }
}

public sealed class AddMonitHistoryCommandValidator : AbstractValidator<AddMonitHistoryCommand>
{
    public AddMonitHistoryCommandValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
        RuleForEach(x => x.Data).SetValidator(new MonitHistoryDtoValidator());
    }

    private class MonitHistoryDtoValidator : AbstractValidator<MonitHistoryDto>
    {
        public MonitHistoryDtoValidator()
        {
            RuleFor(x => x.InstitutionName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.InstitutionBic).NotEmpty().MaximumLength(20);
            RuleFor(x => x.CheckedAt).NotEmpty();
            RuleFor(x => x.ErrorType).MaximumLength(50);
        }
    }
}

public sealed class AddMonitHistoryCommandHandler(IStorageBroker storage) : IRequestHandler<AddMonitHistoryCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(AddMonitHistoryCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entities = request.Data.Select(dto => new MonitHistory
        {
            InstitutionName = dto.InstitutionName,
            InstitutionBic = dto.InstitutionBic,
            Success = dto.Success,
            HttpStatus = dto.HttpStatus,
            Response = dto.Response == null ? null : System.Text.Json.JsonSerializer.Serialize(dto.Response),
            Error = dto.Error,
            ErrorType = dto.ErrorType,
            CheckedAt = dto.CheckedAt.ToUniversalTime(),
            CreatedAt = now
        }).ToList();

        await _storage.MonitHistories.AddRangeAsync(entities, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entities.Count);
    }
}

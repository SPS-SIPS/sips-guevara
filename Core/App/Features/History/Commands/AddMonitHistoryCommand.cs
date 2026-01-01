using System.Text.Json.Serialization;
using SPSS.Domain.Models.Identity;
using SPSS.Domain.Models.BL;

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

public sealed class AddMonitHistoryCommandHandler(IStorageBroker storage, ILogger<AddMonitHistoryCommandHandler> logger) : IRequestHandler<AddMonitHistoryCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<AddMonitHistoryCommandHandler> _logger = logger;

    public async ValueTask<Response<int>> Handle(AddMonitHistoryCommand request, CancellationToken cancellationToken)
    {
        await AutoRegisterMissingParticipantsAsync(request.Data, cancellationToken);

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

    private async Task AutoRegisterMissingParticipantsAsync(List<MonitHistoryDto> data, CancellationToken cancellationToken)
    {
        var incomingBics = data
            .Where(d => !string.IsNullOrWhiteSpace(d.InstitutionBic))
            .Select(d => d.InstitutionBic.ToUpperInvariant())
            .Distinct()
            .ToList();

        if (!incomingBics.Any())
            return;

        var existingBics = await _storage.Participants
            .Where(p => incomingBics.Contains(p.BicNo))
            .Select(p => p.BicNo)
            .ToListAsync(cancellationToken);

        var missingBics = incomingBics.Except(existingBics).ToList();

        if (!missingBics.Any())
            return;

        var systemUser = await GetOrCreateSystemUserAsync(cancellationToken);

        foreach (var bic in missingBics)
        {
            var institutionData = data.First(d => d.InstitutionBic.ToUpperInvariant() == bic);
            var participantId = Guid.NewGuid().ToString();

            var participant = new Participant
            {
                Id = participantId,
                BicNo = bic,
                Name = institutionData.InstitutionName,
                UserId = systemUser.Id,
                CreatedBy = "SYSTEM_AUTO_REGISTER",
                CreatedOn = DateTimeOffset.UtcNow
            };

            await _storage.Participants.AddAsync(participant, cancellationToken);

            _logger.LogInformation(
                "Auto-registered participant from monitoring data: BIC={Bic}, Name={Name}, Id={Id}",
                bic,
                institutionData.InstitutionName,
                participantId);
        }

        await _storage.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetOrCreateSystemUserAsync(CancellationToken cancellationToken)
    {
        const string systemUsername = "system_auto_register";

        var systemUser = await _storage.Users
            .FirstOrDefaultAsync(u => u.Username == systemUsername, cancellationToken);

        if (systemUser != null)
            return systemUser;

        systemUser = new User
        {
            Username = systemUsername,
            Phone = "000000000",
            Email = "system@spss.local",
            Photo = "",
            PasswordHash = "",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedBy = "SYSTEM",
            CreatedOn = DateTimeOffset.UtcNow
        };

        await _storage.Users.AddAsync(systemUser, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created system user for auto-registration: {Username}", systemUsername);

        return systemUser;
    }
}

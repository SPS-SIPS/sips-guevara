namespace SPSS.Domain.Models.DTOs;

public sealed record LoginDto(string AccessToken, Guid RefreshToken, long ExpiresIn, long RefreshExpiresIn);

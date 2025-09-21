namespace CertVal.Application.DTOs;

public record ErrorResponseDto(string Message, Dictionary<string, string[]>? Errors = null);
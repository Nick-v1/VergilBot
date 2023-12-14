using Vergil.Services.Enums;

namespace Vergil.Services.Validation;

public class ValidationReport
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ErrorCode ErrorCode { get; set; }
}
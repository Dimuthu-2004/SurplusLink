namespace SurplusLink.Api.Materials;

public enum MaterialOperationError
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}

public sealed class MaterialOperationException(MaterialOperationError error, string message,
    string? field = null, string? code = null) : Exception(message)
{
    public MaterialOperationError Error { get; } = error;
    public string? Field { get; } = field;
    public string? Code { get; } = code;
}

namespace SurplusLink.Api.Materials;

public enum MaterialOperationError
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}

public sealed class MaterialOperationException(MaterialOperationError error, string message) : Exception(message)
{
    public MaterialOperationError Error { get; } = error;
}

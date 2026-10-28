using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ANDtr.Web.Services;

public sealed record ServiceError(string Field, string Message);

// Outcome of a service operation. Field-level errors map straight onto the form that caused them.
public sealed class ServiceResult(IReadOnlyList<ServiceError> errors)
{
    public static readonly ServiceResult Success = new([]);

    public IReadOnlyList<ServiceError> Errors { get; } = errors;

    public bool Succeeded => Errors.Count == 0;

    public static ServiceResult Fail(string field, string message) => new([new ServiceError(field, message)]);

    public void CopyTo(ModelStateDictionary modelState)
    {
        foreach (var error in Errors)
        {
            modelState.AddModelError(error.Field, error.Message);
        }
    }
}

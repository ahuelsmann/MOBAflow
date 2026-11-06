// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.ModelBinding;

using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Preserves the first header value without splitting commas or normalizing empty strings.
/// </summary>
internal sealed class FirstHeaderValueModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var value = bindingContext.HttpContext.Request.Headers.TryGetValue(bindingContext.ModelName, out var values)
            ? values.FirstOrDefault()
            : null;
        bindingContext.Result = ModelBindingResult.Success(value);
        return Task.CompletedTask;
    }
}

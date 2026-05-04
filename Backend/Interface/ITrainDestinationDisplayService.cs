// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;
using Service;

public interface ITrainDestinationDisplayService
{
    Task UpdateAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken = default);
}

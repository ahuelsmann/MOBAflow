// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Moba.Backend.Manager;
using Moba.Backend.Services;
using Moba.Domain;

public interface IJourneyManagerFactory
{
    JourneyManager Create(IZ21 z21, List<Journey> journeys, ActionExecutionContext? context = null);
}
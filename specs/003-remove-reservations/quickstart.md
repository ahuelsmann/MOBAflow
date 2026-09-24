# Validation guide

Use the SDK from global.json and fake hardware. Do not start MOBAflow or connect a railway.

1. Focused tests: `dotnet test Test/Test.csproj -c Release -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~Interlocking|FullyQualifiedName~SemanticTurnout|FullyQualifiedName~InPortCounter|FullyQualifiedName~JourneyEventPlan|FullyQualifiedName~MobaRuntimeEventPlan"`. Verify nonzero discovery.
2. After host coordination run full portable Release tests, then Windows Release tests with `-f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false`.
3. Build MOBApi/MOBApi.csproj, MOBAflow/MOBAflow.csproj and MOBAsmart/MOBAsmart.csproj with repository Release/analyzer checks. Exclude operator-owned solution input from compile-only checks; report packaging limitations.
4. Verify definition round-trip and UI bindings: direct turnout controls remain; route controls/reserved/locked claims disappear.
5. Run line endings, diff, Spec Kit governance and secrets checks. GitHub CI owns Sonar analysis; publish a draft PR.

Expected: commands independent of occupancy; immutable observational feedback; no serialized routes; counter/journey regressions pass. Manual Light/Dark and hardware checks await separate authorization.

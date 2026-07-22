// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moba.Common.Security;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Security;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class HostCredentialServiceTests
{
    [Test]
    public async Task BootstrapAsync_Should_BeSingleUse_AndIssueHostCapabilities()
    {
        using var context = HostSecurityTestContext.Create();
        var hostCredentials = context.GetRequiredService<IHostCredentialService>();
        var accessTokens = context.GetRequiredService<IControlPlaneAccessTokenService>();

        var exchange = await hostCredentials.BootstrapAsync(context.BootstrapSecret);
        var replay = await hostCredentials.BootstrapAsync(context.BootstrapSecret);
        var token = await accessTokens.IssueAsync(exchange.CredentialId!);
        var principal = await accessTokens.ValidateAsync(token!.Token);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Status, Is.EqualTo(HostCredentialExchangeStatus.Succeeded));
            Assert.That(replay.Status, Is.EqualTo(HostCredentialExchangeStatus.Expired));
            Assert.That(principal, Is.Not.Null);
            Assert.That(principal!.HasClaim(ControlPlaneCapabilities.ClaimType, ControlPlaneCapabilities.HostPublish), Is.True);
            Assert.That(principal.HasClaim(ControlPlaneCapabilities.ClaimType, ControlPlaneCapabilities.HostConsume), Is.True);
            Assert.That(principal.HasClaim(ControlPlaneCapabilities.ClaimType, ControlPlaneCapabilities.RuntimeControl), Is.False);
        });
    }

    [Test]
    public async Task RenewAsync_Should_RevokeHostFamily_WhenConsumedCredentialIsReplayed()
    {
        using var context = HostSecurityTestContext.Create();
        var hostCredentials = context.GetRequiredService<IHostCredentialService>();
        var accessTokens = context.GetRequiredService<IControlPlaneAccessTokenService>();
        var bootstrap = await hostCredentials.BootstrapAsync(context.BootstrapSecret);
        var accessToken = await accessTokens.IssueAsync(bootstrap.CredentialId!);

        var rotation = await hostCredentials.RenewAsync(
            bootstrap.CredentialId!,
            bootstrap.RenewalToken!);
        var replay = await hostCredentials.RenewAsync(
            bootstrap.CredentialId!,
            bootstrap.RenewalToken!);
        var principal = await accessTokens.ValidateAsync(accessToken!.Token);

        Assert.Multiple(() =>
        {
            Assert.That(rotation.Status, Is.EqualTo(HostCredentialExchangeStatus.Succeeded));
            Assert.That(rotation.RenewalToken, Is.Not.EqualTo(bootstrap.RenewalToken));
            Assert.That(replay.Status, Is.EqualTo(HostCredentialExchangeStatus.ReplayDetected));
            Assert.That(principal, Is.Null);
        });
    }

    [Test]
    public async Task BootstrapAsync_Should_StopAfterBoundedInvalidAttempts()
    {
        using var context = HostSecurityTestContext.Create(maximumFailedAttempts: 2);
        var hostCredentials = context.GetRequiredService<IHostCredentialService>();

        var first = await hostCredentials.BootstrapAsync(new string('A', 43));
        var second = await hostCredentials.BootstrapAsync(new string('B', 43));
        var validAfterLimit = await hostCredentials.BootstrapAsync(context.BootstrapSecret);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HostCredentialExchangeStatus.Invalid));
            Assert.That(second.Status, Is.EqualTo(HostCredentialExchangeStatus.RateLimited));
            Assert.That(validAfterLimit.Status, Is.EqualTo(HostCredentialExchangeStatus.RateLimited));
        });
    }

    [Test]
    public async Task AccessToken_Should_BeInvalidAfterHostServiceRestart()
    {
        var root = HostSecurityTestContext.CreateTemporaryRoot();
        string token;
        try
        {
            using (var first = HostSecurityTestContext.Create(root))
            {
                var exchange = await first.GetRequiredService<IHostCredentialService>()
                    .BootstrapAsync(first.BootstrapSecret);
                token = (await first.GetRequiredService<IControlPlaneAccessTokenService>()
                    .IssueAsync(exchange.CredentialId!))!.Token;
            }

            using var second = HostSecurityTestContext.Create(root);
            var principal = await second.GetRequiredService<IControlPlaneAccessTokenService>().ValidateAsync(token);

            Assert.That(principal, Is.Null);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void ProtectedHostOperations_Should_UseSharedCapabilityPolicies()
    {
        Assert.Multiple(() =>
        {
            AssertPolicy<SolutionController>(nameof(SolutionController.PutSolution), ControlPlaneCapabilities.HostPublish);
            AssertPolicy<RuntimeSettingsController>(nameof(RuntimeSettingsController.PutRuntimeSettings), ControlPlaneCapabilities.HostPublish);
            AssertPolicy<RuntimeController>(nameof(RuntimeController.PutSnapshot), ControlPlaneCapabilities.HostPublish);
            AssertPolicy<RuntimeCommandsController>(nameof(RuntimeCommandsController.DequeuePending), ControlPlaneCapabilities.HostConsume);
            AssertPolicy<RuntimeHub>(nameof(RuntimeHub.RegisterHost), ControlPlaneCapabilities.HostConsume);
            AssertPolicy<RuntimeHub>(nameof(RuntimeHub.PushSnapshot), ControlPlaneCapabilities.HostPublish);
        });
    }

    private static void AssertPolicy<T>(string methodName, string expectedPolicy)
    {
        var attribute = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(attribute?.Policy, Is.EqualTo(expectedPolicy), $"Unexpected policy on {typeof(T).Name}.{methodName}");
    }

    private sealed class HostSecurityTestContext : IDisposable
    {
        private readonly bool _ownsRoot;
        private readonly ServiceProvider _services;

        private HostSecurityTestContext(string root, bool ownsRoot, int maximumFailedAttempts)
        {
            Root = root;
            _ownsRoot = ownsRoot;
            BootstrapSecret = HostBootstrapProtocol.CreateSecret();
            var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero));
            var material = HostBootstrapMaterial.Create(
                new HostBootstrapPipeRequest(BootstrapSecret, Environment.ProcessId),
                timeProvider,
                TimeSpan.FromSeconds(30));
            _services = CreateServices(root, maximumFailedAttempts, timeProvider, material);
        }

        public string Root { get; }

        public string BootstrapSecret { get; }

        public static HostSecurityTestContext Create(string? root = null, int maximumFailedAttempts = 5) =>
            new(root ?? CreateTemporaryRoot(), root is null, maximumFailedAttempts);

        public static string CreateTemporaryRoot() =>
            Path.Combine(Path.GetTempPath(), "mobaflow-host-security-tests", Guid.NewGuid().ToString("N"));

        public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

        public void Dispose()
        {
            _services.Dispose();
            if (_ownsRoot && Directory.Exists(Root))
                Directory.Delete(Root, true);
        }

        private static ServiceProvider CreateServices(
            string root,
            int maximumFailedAttempts,
            TimeProvider timeProvider,
            HostBootstrapMaterial material)
        {
            Directory.CreateDirectory(root);
            var settings = new Dictionary<string, string?>
            {
                [$"{ControlPlaneSecurityOptions.SectionName}:StorageDirectory"] = Path.Combine(root, "store"),
                [$"{ControlPlaneSecurityOptions.SectionName}:HostBootstrapMaximumFailedAttempts"] =
                    maximumFailedAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControlPlaneSecurity(configuration, material);
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(root, "keys")));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(timeProvider);
            return services.BuildServiceProvider(validateScopes: true);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
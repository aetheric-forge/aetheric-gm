using AethericGm.Core.Campaigns;
using AethericGm.Core.Characters;
using AethericGm.Core.Dice;
using AethericGm.Core.Profiles;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Packages;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Infrastructure.Characters;
using AethericGm.Infrastructure.Profiles;
using AethericGm.Infrastructure.Rules;
using AethericGm.Infrastructure.Rules.CharacterSheets;
using AethericGm.Infrastructure.Rules.Packages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AethericGm.Infrastructure.Composition;

public static class LocalGmStorageExtensions
{
    /// <summary>
    /// Registers GM's existing SQLite/file adapters without campus or authentication setup.
    /// The host must supply ISshPrivateKeyProtector and call InitializeLocalGmStorageAsync.
    /// </summary>
    public static IServiceCollection AddLocalGmStorage(this IServiceCollection services, LocalGmStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Path.IsPathFullyQualified(options.DataDirectory) || !Path.IsPathFullyQualified(options.RulesCatalogPath))
            throw new ArgumentException("GM storage and rules paths must be absolute.", nameof(options));

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(options.DataDirectory, "aetheric-gm.db")
        }.ToString();
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDiceRandomSource, CryptographicDiceRandomSource>();
        services.TryAddSingleton<IDiceRoller, DiceRoller>();
        services.AddSingleton(new SqliteCampaignRepository(connectionString));
        services.AddSingleton<ICampaignRepository>(sp => sp.GetRequiredService<SqliteCampaignRepository>());
        services.AddSingleton(new SqliteCharacterRepository(connectionString));
        services.AddSingleton<ICharacterRepository>(sp => sp.GetRequiredService<SqliteCharacterRepository>());
        services.AddSingleton<ISshCredentialService>(sp => new SqliteSshCredentialService(
            connectionString, sp.GetRequiredService<ISshPrivateKeyProtector>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IRulesPackageInstaller>(sp => new GitRulesPackageInstaller(
            connectionString, Path.Combine(options.DataDirectory, "RulesPackages"),
            sp.GetRequiredService<ISshCredentialService>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IRulesCatalog>(new FileRulesCatalog(options.RulesCatalogPath));
        services.AddSingleton<ICharacterSheetDefinitionStore>(sp => new FileCharacterSheetDefinitionStore(
            options.RulesCatalogPath, sp.GetRequiredService<IRulesCatalog>()));
        services.AddSingleton<RulesetWorkspaceResolver>();
        return services;
    }

    public static async Task InitializeLocalGmStorageAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(services.GetRequiredService<LocalGmStorageOptions>().DataDirectory);
        await services.GetRequiredService<SqliteCampaignRepository>().InitializeAsync(cancellationToken);
        await services.GetRequiredService<SqliteCharacterRepository>().InitializeAsync(cancellationToken);
        await services.GetRequiredService<ISshCredentialService>().InitializeAsync(cancellationToken);
        await services.GetRequiredService<IRulesPackageInstaller>().InitializeAsync(cancellationToken);
    }
}

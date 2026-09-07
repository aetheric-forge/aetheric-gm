using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Packages;
using AethericGm.Infrastructure.Rules;
using AethericGm.Infrastructure.Rules.CharacterSheets;

namespace AethericGm.Web.Rules;

public sealed class RulesetWorkspaceResolver(
    IRulesCatalog catalog,
    ICharacterSheetDefinitionStore sheetStore,
    IRulesPackageInstaller packageInstaller)
{
    public async Task<RulesetWorkspace?> ResolveAsync(RulesetReference reference, string? ownerSubjectId,
        CancellationToken cancellationToken = default)
    {
        if (ownerSubjectId is not null)
        {
            var package = (await packageInstaller.ListAsync(ownerSubjectId, cancellationToken))
                .FirstOrDefault(item => item.Ruleset == reference);
            if (package is not null)
            {
                var packageCatalog = new FileRulesCatalog(package.PackagePath);
                var descriptor = packageCatalog.Resolve(reference);
                var definition = await new FileCharacterSheetDefinitionStore(package.PackagePath, packageCatalog, true)
                    .GetAsync(reference, cancellationToken);
                return descriptor is null ? null : new RulesetWorkspace(descriptor, definition);
            }
        }

        var builtIn = catalog.Resolve(reference);
        return builtIn is null ? null : new RulesetWorkspace(builtIn, await sheetStore.GetAsync(reference, cancellationToken));
    }
}

public sealed record RulesetWorkspace(RulesetDescriptor Descriptor, CharacterSheetDefinition? CharacterSheet);

using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using AethericForge.Runtime.Models.Institutions;

namespace AethericGm.Institutions.Gm;

public sealed class AethericGmContext(
    IInstitutionTemplate template,
    IServiceProvider services,
    IInstitution parent)
    : InstitutionContext(template, services, parent ?? throw new ArgumentNullException(nameof(parent)));

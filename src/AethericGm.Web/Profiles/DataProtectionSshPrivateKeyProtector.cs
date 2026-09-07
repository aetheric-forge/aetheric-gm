using AethericGm.Core.Profiles;
using Microsoft.AspNetCore.DataProtection;

namespace AethericGm.Web.Profiles;

public sealed class DataProtectionSshPrivateKeyProtector(IDataProtectionProvider provider) : ISshPrivateKeyProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("AethericGm.SshPrivateKey.v1");
    public string Protect(string privateKey) => protector.Protect(privateKey);
    public string Unprotect(string protectedPrivateKey) => protector.Unprotect(protectedPrivateKey);
}

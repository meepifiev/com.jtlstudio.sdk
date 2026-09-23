using System.Collections.Generic;
namespace JTLStudio.SDK.Providers
{
    public interface IFlagsProvider : IProvider
    {
        void Configure(IReadOnlyList<FlagDefinition> flags);
        bool TryGetValue(string key, out string value);
    }
}

using Gommon;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Services.GitLab;

public class PinnedVersions : SafeDictionary<SupportedPlatform, SafeDictionary<SupportedArchitecture, string>>
{
    public PinnedVersions(VersionCache vcache, IConfigurationSection configSection)
    {
        foreach (var subSection in configSection.GetChildren())
        {
            if (!subSection.Key.TryParseAsSupportedPlatform(out var platform))
            {
                vcache.Logger.LogWarning("Unknown platform '{key}'; skipping", subSection.Key);
                continue;
            }

            SafeDictionary<SupportedArchitecture, string> versions = [];
            foreach (var archVersionPair in subSection.GetChildren())
            {
                if (!archVersionPair.Key.TryParseAsSupportedArchitecture(out SupportedArchitecture arch))
                {
                    vcache.Logger.LogWarning("Unknown platform '{key}' in '{subsectionName}'; skipping", archVersionPair.Key, subSection.Key);
                    continue;
                }

                if (archVersionPair.Value == null)
                {
                    vcache.Logger.LogWarning("Version pair value for '{key}' in '{subsectionName}' was null; skipping", archVersionPair.Key, subSection.Key);
                    continue;
                }

                versions[arch] = archVersionPair.Value;
            }

            this[platform] = versions;
        }
    }

    public string? Find(SupportedPlatform plat, SupportedArchitecture arch) 
        => this[plat]?[arch];
}
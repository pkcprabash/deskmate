using System;
using System.Collections.Generic;

namespace Deskmate.Infrastructure.Avatars;

/// <summary>An avatar pack that can't be used, with every problem found listed in the message.</summary>
public sealed class InvalidAvatarPackException(string packName, IReadOnlyList<string> problems)
    : Exception($"Avatar pack '{packName}' is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}")
{
    public string PackName { get; } = packName;
    public IReadOnlyList<string> Problems { get; } = problems;
}

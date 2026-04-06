using UnityEngine;

namespace TONX.Achievements.Core.Interfaces;

public interface IAchievement
{
    int Id { get; }
    string Name { get; }
    string Description { get; }
    string TitleDisplay { get; }
    Color TitleColor { get; }
    string TitleColorHex { get; }
}

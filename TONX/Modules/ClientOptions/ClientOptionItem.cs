using BepInEx.Configuration;
using UnityEngine;

namespace TONX.Modules.ClientOptions;

public sealed class ClientOptionItem<T> : ClientActionItem
{
    private ClientOptionItem(
        string name,
        ConfigEntry<T> config,
        OptionsMenuBehaviour optionsMenuBehaviour)
        : base(
            name,
            optionsMenuBehaviour)
    {
        Config = config;
        UpdateToggle();
    }

    private ConfigEntry<T> Config { get; }

    /// <summary>
    /// Modオプション画面にconfigのトグルを追加します
    /// </summary>
    /// <param name="name">ボタンラベルの翻訳キーとボタンのオブジェクト名</param>
    /// <param name="config">対応するconfig</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviourのインスタンス</param>
    /// <param name="additionalOnClickAction">クリック時に追加で発火するアクション．configが変更されたあとに呼ばれる</param>
    /// <returns>作成したアイテム</returns>
    public static ClientOptionItem<T> Create(
        string name,
        ConfigEntry<T> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        var item = new ClientOptionItem<T>(name, config, optionsMenuBehaviour);
        item.OnClickAction = () =>
        {
            switch (config.Value)
            {
                case bool:
                    config.Value = (T)(object)!(bool)(object)config.Value;
                    break;
                case not null when typeof(T).IsEnum:
                    var allValues = (T[])Enum.GetValues(typeof(T));
                    if (allValues.Length == 0) break;
                    var currentIndex = Array.IndexOf(allValues, config.Value);
                    if (currentIndex < 0)
                        currentIndex = 0;
                    else
                        currentIndex = (currentIndex + 1) % allValues.Length;
                    config.Value = allValues[currentIndex];
                    item.ToggleButton.Text.text += $"\n|{GetString(config.Value.ToString())}|";
                    break;
            }

            item.UpdateToggle();
            additionalOnClickAction?.Invoke();
        };
        return item;
    }

    private void UpdateToggle()
    {
        if (!ToggleButton) return;

        var color = new Color32(77, 77, 77, byte.MaxValue);
        switch (Config.Value)
        {
            case bool value:
                color = value
                    ? new Color32(255, 129, 166, byte.MaxValue)
                    : new Color32(77, 77, 77, byte.MaxValue);
                break;
            case not null when typeof(T).IsEnum:
                var allValues = (T[])Enum.GetValues(typeof(T));
                if (allValues.Length == 0) break;
                var currentIndex = Array.IndexOf(allValues, Config.Value);

                var baseColor = new Color32(255, 129, 166, byte.MaxValue);
                var factor = allValues.Length > 1
                    ? currentIndex / (float)(allValues.Length - 1)
                    : 0f;
                var newRed = (byte)Mathf.Clamp(baseColor.r - (byte)(factor * 70), 0, 255);
                color = new Color32(
                    newRed,
                    baseColor.g,
                    baseColor.b,
                    baseColor.a
                );

                Config.Value = allValues[currentIndex];
                Rename();
                ToggleButton.Text.text += $"\n|{GetString($"Value.{Config.Value.ToString()}")}|";
                break;
        }

        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}
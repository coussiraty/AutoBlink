using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Helpers;
using ExileCore2.Shared.Nodes;
using static ExileCore2.Shared.Nodes.HotkeyNodeV2;
using Graphics = ExileCore2.Graphics;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AutoBlink;

public class AutoBlink : BaseSettingsPlugin<AutoBlinkSettings>
{
    private bool _isActive = false;
    private readonly string _blinkSkillName = "BlinkPlayer";
    private Helpers helpers;
    private Camera Camera => GameController.IngameState.Camera;
    private bool passiveTreeOpen = false;
    private IngameUIElements IngameUi => GameController.IngameState.IngameUi;
    ActorSkill blinkSkill = null;
    private bool initialised = false;
    private bool runPlugin =>
        Settings.Enable
        && initialised
        && Settings.IgnoreUIElements
        || (!IngameUi.TreePanel.IsVisible
            && !IngameUi.ChatTitlePanel.IsVisible);
    private int safetyDelay => Settings.SafetyDelay;
    private int  longestCastDelay => Settings.LongestCastDelay;
    private int blinkAnimationDelay => Settings.BlinkAnimationDelay;

    private int targetWeaponSet => Settings.WeaponSet == "2" ? 1 : 0;

    private float blinkCooldown = 1000;
    private readonly Stopwatch _blinkCooldownStopWatch = Stopwatch.StartNew();
    private bool blinkInCooldown => _blinkCooldownStopWatch.ElapsedMilliseconds <= blinkCooldown;

    private HotkeyNode keyBlink => Settings.KeyAutoBlink;
    private HotkeyNodeValue keyWeaponSwap => Settings.KeyWeaponSwap.Value;
    private HotkeyNodeValue keyDodgeRoll => Settings.KeyDodgeRoll.Value;

    private int sourceWeaponSet = 0;
    private bool weaponSetChanged = false;
    private string imageFilename = "blink.png";
    private const string imagePath = "images\\blink.png";
    private IntPtr imageId;

    public override bool Initialise()
    {
        initialised = true;

        return base.Initialise();
    }

    public override void Tick()
    {
        if (!runPlugin) return;

        if (!LoadBlinkSkill()) return;

        RestoreSourceWeaponSet();
        RunAutoBlink();
    }

    public override void Render()
    {
        if (!runPlugin) return;

        if (!LoadBlinkSkill()) return;

        RenderTextBlink();
        RenderTextWeaponSet();
        RenderImageBlink();
    }
    public override void AreaChange(AreaInstance area)
    {
        base.AreaChange(area);
        GameController.PluginBridge.SaveMethod("AutoBlink.IsActive", () => _isActive);  // Fixed capitalization
        LogMessage("AutoBlink.IsActive registered in PluginBridge (via AreaChange)");  // Fixed log message too
    }
    private void RenderTextWeaponSet()
    {
        if (!Settings.Render.WeaponSet.Text.Enabled
            || !Settings.Render.WeaponSet.Text.ShowInTown && GameController.Area.CurrentArea.IsTown
            || !Settings.Render.WeaponSet.Text.ShowInHideout && GameController.Area.CurrentArea.IsHideout)
        {
            return;
        }

        int activeWeaponSet = GetActiveWeaponSet();

        if (activeWeaponSet != targetWeaponSet && !Settings.Render.WeaponSet.Text.AlwaysShow) return;

        var text = activeWeaponSet == 0
            ? Settings.Render.WeaponSet.Text.WeaponSet1Text
            : Settings.Render.WeaponSet.Text.WeaponSet2Text;

        var color = activeWeaponSet == 0
            ? Settings.Render.WeaponSet.Text.WeaponSet1Color
            : Settings.Render.WeaponSet.Text.WeaponSet2Color;

        if (Settings.Render.WeaponSet.Text.Background)
        {
            float bgPositionLeft = Settings.Render.WeaponSet.Text.PositionX;
            float bgPositionTop = Settings.Render.WeaponSet.Text.PositionY;

            ColorNode bgColor = Settings.Render.WeaponSet.Text.BackgroundColor;

            helpers.DrawBackgroundRectangle(Graphics, text, bgColor, bgPositionLeft, bgPositionTop);
        }

        float positionX = Settings.Render.WeaponSet.Text.PositionX;
        float positionY = Settings.Render.WeaponSet.Text.PositionY;

        helpers.DrawText(Graphics, text, color, positionX, positionY);
    }

    private void RenderTextBlink()
    {
        if (!Settings.Render.Blink.Text.Enabled
            || !Settings.Render.Blink.Text.ShowInTown && GameController.Area.CurrentArea.IsTown
            || !Settings.Render.Blink.Text.ShowInHideout && GameController.Area.CurrentArea.IsHideout)
        {
            return;
        }

        bool isBlinkInCooldown = helpers.IsBlinkInCooldown(GameController);

        if (isBlinkInCooldown && !Settings.Render.Blink.Text.AlwaysShow) return;

        var text = Settings.Render.Blink.Text.AvailableText;
        var color = Settings.Render.Blink.Text.AvailableColor;

        if (Settings.Render.Blink.Text.AlwaysShow)
        {
            text = isBlinkInCooldown
                ? Settings.Render.Blink.Text.UnavailableText
                : Settings.Render.Blink.Text.AvailableText;

            color = isBlinkInCooldown
                ? Settings.Render.Blink.Text.UnavailableColor
                : Settings.Render.Blink.Text.AvailableColor;
        }

        if (Settings.Render.Blink.Text.Background)
        {
            float bgPositionLeft = Settings.Render.Blink.Text.PositionX;
            float bgPositionTop = Settings.Render.Blink.Text.PositionY;

            ColorNode bgColor = Settings.Render.Blink.Text.BackgroundColor;

            helpers.DrawBackgroundRectangle(Graphics, text, bgColor, bgPositionLeft, bgPositionTop);
        }

        float textPositionX = Settings.Render.Blink.Text.PositionX;
        float textPositionY = Settings.Render.Blink.Text.PositionY;

        helpers.DrawText(Graphics, text, color, textPositionX, textPositionY);
    }

    private void RenderImageBlink()
    {
        if (!Settings.Render.Blink.Image.Enabled
            || !Settings.Render.Blink.Image.ShowInTown && GameController.Area.CurrentArea.IsTown
            || !Settings.Render.Blink.Image.ShowInHideout && GameController.Area.CurrentArea.IsHideout)
        {
            return;
        }

        bool isBlinkInCooldown = helpers.IsBlinkInCooldown(GameController);

        var imgColor = isBlinkInCooldown
            ? Settings.Render.Blink.Image.ColorCooldown
            : Settings.Render.Blink.Image.ColorAvailable;

        float imgPosX = Settings.Render.Blink.Image.PositionX;
        float imgPosY = Settings.Render.Blink.Image.PositionY;
        float imgSizeX = Settings.Render.Blink.Image.SizeX;
        float imgSizeY = Settings.Render.Blink.Image.SizeY;

        helpers.DrawImage(Graphics, DirectoryFullName, imagePath, imageFilename, imgPosX, imgPosY, imgSizeX, imgSizeY, imgColor);
    }


private async void RunAutoBlink()
{
    bool autoBlinkHotkeyPressed = Input.IsKeyDown(keyBlink);

    if (autoBlinkHotkeyPressed && !_isActive)
    {
        _isActive = true;
        LogMessage("[AutoBlink] Space pressionado - Habilitando ação");

        int activeWeaponSet = GetActiveWeaponSet();

        if (!blinkInCooldown && blinkSkill.CanBeUsed)
        {
            if (activeWeaponSet != targetWeaponSet)
            {
                LogMessage($"[AutoBlink] Trocando para Weapon Set {targetWeaponSet + 1}...");
                InputHelper.SendInputPress(keyWeaponSwap);

                await Task.Delay(100);

                if (GetActiveWeaponSet() != targetWeaponSet)
                {
                    LogMessage("[AutoBlink] Falha na troca de armas, tentativa abortada.");
                    _isActive = false;
                    return;
                }

                LogMessage("[AutoBlink] Weapon Set trocado com sucesso!");
            }

            LogMessage("[AutoBlink] Usando Blink!");
            InputHelper.SendInputPress(keyDodgeRoll);

            _blinkCooldownStopWatch.Restart();
        }
        else
        {
            int alternativeWeaponSet = targetWeaponSet == 0 ? 1 : 0;

            if (activeWeaponSet != alternativeWeaponSet)
            {
                LogMessage($"[AutoBlink] Trocando para Weapon Set {alternativeWeaponSet + 1} (Dodge Normal)...");
                InputHelper.SendInputPress(keyWeaponSwap);

                await Task.Delay(100);

                if (GetActiveWeaponSet() != alternativeWeaponSet)
                {
                    LogMessage("[AutoBlink] Falha na troca de armas para o Dodge normal.");
                    _isActive = false;
                    return;
                }

                LogMessage("[AutoBlink] Weapon Set trocado para Dodge Normal!");
            }

            LogMessage("[AutoBlink] Blink em cooldown, usando Dodge normal.");
            InputHelper.SendInputPress(keyDodgeRoll);
        }
    }

    if (!autoBlinkHotkeyPressed)
    {
        _isActive = false;
    }
}
    private async Task PressKey(HotkeyNodeValue key)
    {
        InputHelper.SendInputPress(key);
        await Task.Delay(safetyDelay);
    }

    private int GetActiveWeaponSet()
    {
        Stats stats = GameController.Player.GetComponent<Stats>();

        return stats?.ActiveWeaponSetIndex ?? 0;
    }

    public async void RestoreSourceWeaponSet()
    {
        if (weaponSetChanged)
        {
            int activeWeaponSet = GetActiveWeaponSet();

            if (activeWeaponSet != sourceWeaponSet)
            {
                PressKey(keyWeaponSwap);
            }

            weaponSetChanged = false;
        }
    }

    public bool LoadBlinkSkill()
    {
        if (!GameController.Player.HasComponent<Actor>()) return false;

        helpers = new Helpers(_blinkSkillName);

        blinkSkill = helpers.FetchBlinkSkill(GameController);

        if (blinkSkill == null) return false;

        blinkCooldown = blinkSkill.Cooldown;

        return true;
    }
}

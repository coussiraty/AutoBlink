using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Helpers;
using ExileCore2.Shared.Nodes;
using static ExileCore2.Shared.Nodes.HotkeyNodeV2;
using Graphics = ExileCore2.Graphics;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AutoBlink;

public class AutoBlink : BaseSettingsPlugin<AutoBlinkSettings>
{
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

        if(!LoadBlinkSkill()) return;

        RestoreSourceWeaponSet();
        RunAutoBlink();
    }

    public override void Render()
    {
        if (!runPlugin) return;

        if(!LoadBlinkSkill()) return;

        RenderTextBlink();
        RenderTextWeaponSet();
        RenderImageBlink();
    }

    private void RenderTextWeaponSet()
    {        
        if (!Settings.Render.WeaponSet.WarningText.Enabled
            || !Settings.Render.WeaponSet.WarningText.ShowInTown && GameController.Area.CurrentArea.IsTown
            || !Settings.Render.WeaponSet.WarningText.ShowInHideout && GameController.Area.CurrentArea.IsHideout)
        {
            return;
        }

        int activeWeaponSet = GetActiveWeaponSet();

        if(activeWeaponSet != targetWeaponSet) return;

        var text = activeWeaponSet == 0
            ? Settings.Render.WeaponSet.WarningText.WeaponSet1Text
            : Settings.Render.WeaponSet.WarningText.WeaponSet2Text;

        var color = Settings.Render.WeaponSet.WarningText.TextColor;

        if (Settings.Render.WeaponSet.WarningText.Background)
        {
            float bgPositionLeft = Settings.Render.WeaponSet.WarningText.PositionX;
            float bgPositionTop = Settings.Render.WeaponSet.WarningText.PositionY;

            ColorNode bgColor = Settings.Render.WeaponSet.WarningText.BackgroundColor;

            helpers.DrawBackgroundRectangle(Graphics, text, bgColor, bgPositionLeft, bgPositionTop);
        }

        float positionX = Settings.Render.WeaponSet.WarningText.PositionX;
        float positionY = Settings.Render.WeaponSet.WarningText.PositionY;

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

        if(isBlinkInCooldown) return;

        var text = Settings.Render.Blink.Text.AvailableText;

        var color = Settings.Render.Blink.Text.AvailableColor;

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

    private void RunAutoBlink()
    {
        bool autoBlinkHotkeyPressed = Input.IsKeyDown(keyBlink);

        if (autoBlinkHotkeyPressed)
        {
            if (!blinkInCooldown && blinkSkill.CanBeUsed)
            {
                // ActorSkill blinkSkill = helpers.FetchBlinkSkill(GameController);

                // if (blinkSkill == null) 
                // {
                //     DebugWindow.LogError("The plugin could not find the Blink skill. Please check your skills and try again.");
                //     return;

                // }

                try
                {
                    sourceWeaponSet = GetActiveWeaponSet();
                    DebugWindow.LogMsg("Current WeaponSet: " + GetActiveWeaponSet().ToString());

                    if (targetWeaponSet != sourceWeaponSet)
                    {
                        PressKey(keyWeaponSwap);
                        weaponSetChanged = true;
                    }

                    // use Blink Skill by pressing dodge roll key
                    // after blink needs a minimal extra delay before any other actions to account for the animation
                    PressKey(keyDodgeRoll);

                    // Add a safety delay in between key presses
                    Thread.Sleep(blinkAnimationDelay);

                    _blinkCooldownStopWatch.Restart();
                }
                catch (Exception ex)
                {
                    DebugWindow.LogError($"{ex.Message}");
                }
            }
        }
    }

    private void PressKey(HotkeyNodeValue key)
    {
        InputHelper.SendInputPress(key);
        Thread.Sleep(safetyDelay);
    }

    private int GetActiveWeaponSet()
    {
        Stats stats = GameController.Player.GetComponent<Stats>();

        return stats?.ActiveWeaponSetIndex ?? 0;
    }

    public void RestoreSourceWeaponSet()
    {
        if (weaponSetChanged)
        {
            int activeWeaponSet = GetActiveWeaponSet();
            DebugWindow.LogMsg("Current WeaponSet: " + GetActiveWeaponSet().ToString());
            if (activeWeaponSet != sourceWeaponSet)
            {
                PressKey(keyWeaponSwap);
                DebugWindow.LogMsg("WeaponSet changed to: " + sourceWeaponSet.ToString());
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
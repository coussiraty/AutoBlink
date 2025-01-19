using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared;
using ExileCore2.Shared.Helpers;
using static ExileCore2.Shared.Nodes.HotkeyNodeV2;
using Graphics = ExileCore2.Graphics;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Vector2 = System.Numerics.Vector2;
using ExileCore2.Shared.Nodes;

namespace AutoBlink;

public class AutoBlink : BaseSettingsPlugin<AutoBlinkSettings>
{
    private int safetyDelay => Settings.BlinkSettings.SafetyDelay;
    private readonly string _blinkSkillName = "BlinkPlayer";
    private float blinkCooldown = 0;
    private readonly Stopwatch _blinkCooldownStopWatch = Stopwatch.StartNew();
    private bool BlinkInCooldown => _blinkCooldownStopWatch.ElapsedMilliseconds <= blinkCooldown;
    private HotkeyNode blinkHotKey => Settings.BlinkSettings.BlinkHotkey;
    private HotkeyNodeValue weaponSetSwapKey => Settings.BlinkSettings.WeaponSetSwapKey.Value;
    private HotkeyNodeValue dodgeKey => Settings.BlinkSettings.DodgeKey.Value;
    private int blinkWeaponSet => Settings.BlinkSettings.BlinkWeaponSet == "1" ? 0 : 1;

    public override bool Initialise()
    {
        Actor actor = GameController.Player.GetComponent<Actor>();
        List<ActorSkill> actorSkills = actor.ActorSkills;
        ActorSkill blinkSkill = actorSkills.FirstOrDefault(s => s.Name == _blinkSkillName);

        // set blink cooldown
        blinkCooldown = blinkSkill.Cooldown;

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        //Perform once-per-zone processing here
        //For example, Radar builds the zone map texture here
    }

    public override void Tick()
    {
        if (!Settings.Enable)
        {
            return;
        }

        if (!GameController.Window.IsForeground())
        {
            return;
        }

        if (!GameController.Player.HasComponent<Actor>())
        {
            DebugWindow.LogError("Cannot find player Actor component");
            return;
        }

        RunAutoBlink();
    }

    public override void Render()
    {
        ActorSkill blink = getActorSkill(_blinkSkillName);
        bool isOnCooldown = blink.IsOnCooldown;

        var notificationText = isOnCooldown
            ? Settings.NotificationSettings.UnavailableText
            : Settings.NotificationSettings.AvailableText;

        var notificationColor = isOnCooldown
            ? Settings.NotificationSettings.UnavailableColor
            : Settings.NotificationSettings.AvailableColor;

        var notificationTextArea = Graphics.MeasureText(notificationText);
        
        DrawBackground(notificationTextArea);
        DrawNotification(notificationText, notificationColor);
    }

    private void DrawBackground(Vector2 textArea)
    {
        if (!Settings.NotificationSettings.Background) return;

        float margin = 15;

        float positionLeft = Settings.NotificationSettings.NotificationPositionX;
        float positionTop = Settings.NotificationSettings.NotificationPositionY;

        float positionRight = positionLeft + textArea.X + margin;
        float positionBottom = positionTop + textArea.Y + margin;

        Graphics.DrawBox(new RectangleF
        {
            Left = positionLeft,
            Top = positionTop,
            Right = positionRight,
            Bottom = positionBottom,
        }, Settings.NotificationSettings.BackgroundColor);
    }

    private void DrawNotification(string notificationText, ColorNode notificationColor)
    {            
        float padding = 5;

        float positionX = Settings.NotificationSettings.NotificationPositionX + padding;
        float positionY = Settings.NotificationSettings.NotificationPositionY + padding;

        var textArea = new Vector2(positionX, positionY);
        
        Graphics.DrawText(notificationText, textArea, notificationColor);
    }
   
    private void autoBlink()
    {
        try
        {
            bool weaponSetChanged = false;
            int sourceWeaponSet = getActiveWeaponSet();
            int targetWeaponSet = blinkWeaponSet;

            if (targetWeaponSet != sourceWeaponSet)
            {
                pressKey(weaponSetSwapKey);
                weaponSetChanged = true;
            }

            pressKey(dodgeKey);
            _blinkCooldownStopWatch.Restart();

            // Add a safety delay in between key presses
            Thread.Sleep(safetyDelay + 100);

            if (weaponSetChanged)
            {
                pressKey(weaponSetSwapKey);
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"{ex.Message}");
        }
    }

    private int getActiveWeaponSet()
    {
        Stats stats = GameController.Player.GetComponent<Stats>();

        return stats?.ActiveWeaponSetIndex ?? -1;
    }

    private void pressKey(HotkeyNodeValue key)
    {
        InputHelper.SendInputPress(key);

        // Add a safety delay in between key presses
        Thread.Sleep(safetyDelay);
    }
    
    private ActorSkill getActorSkill(string skillName = null)
    {
        Actor _actor = GameController.Player.GetComponent<Actor>();
        List<ActorSkill> actorSkills = _actor.ActorSkills;

        if (actorSkills == null)
        {
            DebugWindow.LogError("Could not load Character skills. Try reloading Core");
            return null;
        }

        return actorSkills.FirstOrDefault(s => s.Name == skillName);
    }

    private void getSkillDetails(ActorSkill skill, List<string> properties = null)
    {
        if (skill == null)
        {
            DebugWindow.LogError("No valid skill selected. Nothing to show.");
            return;
        }

        DebugWindow.LogMsg($"=> Attributes of {skill.Name}:");

        // Get all public properties
        PropertyInfo[] skillProperties = skill.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        bool showAll = properties == null || properties.Count == 0;

        foreach (PropertyInfo property in skillProperties)
        {
            // Skip if not showing all and this property is not in the list
            if (!showAll && !properties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                object value = property.GetValue(skill);
                string valueString = value?.ToString() ?? "null";

                DebugWindow.LogMsg($"{property.Name}: {valueString}");
            }
            catch (Exception ex)
            {
                DebugWindow.LogMsg($"{property.Name}: Error getting value - {ex.Message}");
            }
        }

    }

    private void RunAutoBlink()
    {

        bool autoBlinkHotkeyPressed = Input.IsKeyDown(blinkHotKey);

        if (autoBlinkHotkeyPressed)
        {
            if (!BlinkInCooldown)
            {
                ActorSkill blinkSkill = getActorSkill(_blinkSkillName);

                if (blinkSkill == null)
                {
                    DebugWindow.LogError("Blink skill was not found");
                    return;
                }

                if (!blinkSkill.CanBeUsed)
                {
                    return;
                }

                autoBlink();
            }
        }
    }
}

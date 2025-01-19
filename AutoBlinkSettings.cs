using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;
using System.Drawing;

namespace AutoBlink;

public class AutoBlinkSettings : ISettings
{
    public BlinkSettings BlinkSettings { get; set; } = new BlinkSettings();
    public NotificationSettings NotificationSettings { get; set; } = new NotificationSettings();
    public WeaponSetVisualSettings WeaponSetVisualSettings { get; set; } = new WeaponSetVisualSettings();
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
}

[Submenu]
public class BlinkSettings
{
    public HotkeyNode BlinkHotkey { get; set; } = new(Keys.V);
    public HotkeyNodeV2 WeaponSetSwapKey { get; set; } = new(Keys.X);
    public HotkeyNodeV2 DodgeKey { get; set; } = new(Keys.Space);
    
    [Menu(null, "In milliseconds")]
    public RangeNode<int> SafetyDelay { get; set; } = new(250, 0, 2000);
    
    [Menu(null, "Which WeaponSet is the Blink skill")]
    public TextNode BlinkWeaponSet { get; set; } = "2";
}

[Submenu]
public class NotificationSettings
{
    public ToggleNode Enabled { get; set; } = new ToggleNode(true);
    public TextNode AvailableText { get; set; } = "BLINK READY";
    public ColorNode AvailableColor { get; set; } = Color.Green;
    public TextNode UnavailableText { get; set; } = "COOLDOWN";
    public ColorNode UnavailableColor { get; set; } = Color.Red;
    public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(1233, 0, 2000);
    public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(434, 0, 2000);
    public ToggleNode Background { get; set; } = new ToggleNode(true);
    public ColorNode BackgroundColor { get; set; } = Color.FromArgb(197, 0, 0, 0);
}

[Submenu]
public class WeaponSetVisualSettings
{
    public ToggleNode Enabled { get; set; } = new ToggleNode(true);
    public ColorNode WeaponSet1Color { get; set; } = Color.Red;
    public ColorNode WeaponSet2Color { get; set; } = Color.Green;
    public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(1233, 0, 2000);
    public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(401, 0, 2000);
    public ToggleNode Background { get; set; } = new ToggleNode(true);
    public ColorNode BackgroundColor { get; set; } = Color.FromArgb(197, 0, 0, 0);
}
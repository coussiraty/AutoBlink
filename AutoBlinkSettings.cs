using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;
using System.Drawing;

namespace AutoBlink;

public class AutoBlinkSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu(null, "Keyboard key to fire up the AutoBlink Plugin")]
    public HotkeyNode KeyAutoBlink { get; set; } = new(Keys.V);
    
    [Menu(null, "In game Input key for Weapon Swap")]
    public HotkeyNodeV2 KeyWeaponSwap { get; set; } = new(Keys.X);
    
    [Menu(null, "In game Input key for Dodge Roll")]
    public HotkeyNodeV2 KeyDodgeRoll { get; set; } = new(Keys.Space);
    
    [Menu(null, "In milliseconds")]
    public RangeNode<int> SafetyDelay { get; set; } = new(250, 100, 2000);

    [Menu(null, "In milliseconds")]
    public RangeNode<int> BlinkAnimationDelay { get; set; } = new(400, 400, 2000);

    [Menu(null, "In milliseconds")]
    public RangeNode<int> LongestCastDelay { get; set; } = new(50, 320, 1000);
    
    [Menu(null, "Which WeaponSet is your Blink skill")]
    public TextNode WeaponSet { get; set; } = "2";

    [Menu(null, "Continue to show visuals even when Chat or Passive Tree panel are open")]
    public ToggleNode IgnoreUIElements { get; set; } = new ToggleNode(false);
    
    public Render Render { get; set; } = new Render();
}

[Submenu]
public class Render
{
    public Blink Blink { get; set; } = new Blink();
    public WeaponSet WeaponSet { get; set; } = new WeaponSet();
}

[Submenu]
public class Blink
{
    public BlinkText Text { get; set; } = new BlinkText();
    public BlinkImage Image { get; set; } = new BlinkImage();
}

[Submenu]
public class BlinkText
{
    public ToggleNode Enabled { get; set; } = new ToggleNode(true);

    [Menu(null, "This will either make the alert fixed switching colors or only displays it when Blink is available.")]
    public ToggleNode AlwaysShow { get; set; } = new ToggleNode(true);
    public ToggleNode ShowInTown { get; set; } = new ToggleNode(false);
    public ToggleNode ShowInHideout { get; set; } = new ToggleNode(true);
    public TextNode AvailableText { get; set; } = "BLINK READY";
    public ColorNode AvailableColor { get; set; } = Color.Green;
    public TextNode UnavailableText { get; set; } = "COOLDOWN";
    public ColorNode UnavailableColor { get; set; } = Color.Red;
    public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(1230, 0, 2000);
    public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(434, 0, 2000);
    public ToggleNode Background { get; set; } = new ToggleNode(true);
    public ColorNode BackgroundColor { get; set; } = Color.FromArgb(197, 0, 0, 0);
}

[Submenu]
public class BlinkImage
{
    public ToggleNode Enabled { get; set; } = new ToggleNode(true);

    public ToggleNode ShowInTown { get; set; } = new ToggleNode(false);
    public ToggleNode ShowInHideout { get; set; } = new ToggleNode(true);    
    public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(64, 0, 2000);
    public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(64, 0, 2000);
    public RangeNode<int> SizeX { get; set; } = new RangeNode<int>(1900, 0, 2000);
    public RangeNode<int> SizeY { get; set; } = new RangeNode<int>(1350, 0, 2000);
    public ColorNode ColorAvailable { get; set; } = Color.White;
    public ColorNode ColorCooldown { get; set; } = Color.Red;
}

[Submenu]
public class WeaponSet
{
    [Menu(null, "A visual message will appear if the current WeaponSet selected is different from your main one.")]
    public WeaponSetText Text { get; set; } = new WeaponSetText();
}

[Submenu]
public class WeaponSetText
{
    public ToggleNode Enabled { get; set; } = new ToggleNode(true);

    [Menu(null, "Always show currently Weapon Set or just alert when is not the main one.")]
    public ToggleNode AlwaysShow { get; set; } = new ToggleNode(true);
    public ToggleNode ShowInTown { get; set; } = new ToggleNode(false);
    public ToggleNode ShowInHideout { get; set; } = new ToggleNode(true);
    public TextNode WeaponSet1Text { get; set; } = "WeaponSet 1";
    public ColorNode WeaponSet1Color { get; set; } = Color.Red;
    public TextNode WeaponSet2Text { get; set; } = "WeaponSet 2";
    public ColorNode WeaponSet2Color { get; set; } = Color.Green;
    public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(1230, 0, 2000);
    public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(401, 0, 2000);
    public ToggleNode Background { get; set; } = new ToggleNode(true);
    public ColorNode BackgroundColor { get; set; } = Color.FromArgb(197, 0, 0, 0);
}

using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Helpers;
using ExileCore2.Shared.Nodes;

using Graphics = ExileCore2.Graphics;
using Vector2 = System.Numerics.Vector2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;


public class Helpers
{
    private float _graphicsBackgroundMargin = 15;
    private float _graphicsTextPadding = 5;
    private string _blinkSkillName;

    public Helpers(string blinkSkillName)
    {
        this._blinkSkillName = blinkSkillName;
    }

    private ActorSkill FetchActorSkill(GameController controller, string skillName)
    {
        Actor actor = controller.Player.GetComponent<Actor>();
        List<ActorSkill> actorSkills = actor.ActorSkills;

        if (actorSkills == null)
        {
            DebugWindow.LogError("Could not load Character skills. Try reloading Core");
            return null;
        }

        return actorSkills.FirstOrDefault(s => s.Name == skillName);
    }

    public ActorSkill FetchBlinkSkill(GameController controller)
    {
        Actor actor = controller.Player.GetComponent<Actor>();
        List<ActorSkill> actorSkills = actor.ActorSkills;

        if (actorSkills == null)
        {
            DebugWindow.LogError("Could not load Character skills. Try reloading Core");
            return null;
        }

        return actorSkills.FirstOrDefault(s => s.Name == _blinkSkillName);
    }

    private void ShowActorSkillDetails(ActorSkill skill, List<string> properties = null)
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

    public bool IsBlinkInCooldown(GameController controller)
    {
        ActorSkill blink = FetchActorSkill(controller, _blinkSkillName);
        return blink.IsOnCooldown;
    }

    public void DrawBackgroundRectangle(Graphics graphics, string text, ColorNode color, float positionLeft, float positionTop)
    {
        var backgroundArea = graphics.MeasureText(text);

        float positionRight = positionLeft + backgroundArea.X + _graphicsBackgroundMargin;
        float positionBottom = positionTop + backgroundArea.Y + _graphicsBackgroundMargin;

        graphics.DrawBox(new RectangleF
        {
            Left = positionLeft,
            Top = positionTop,
            Right = positionRight,
            Bottom = positionBottom,
        }, color);
    }

    public void DrawText(Graphics graphics, string text, ColorNode color, float positionX, float positionY)
    {
        float posX = positionX + _graphicsTextPadding;
        float posY = positionY + _graphicsTextPadding;

        Vector2 textArea = new Vector2(posX, posY);

        graphics.DrawText(text, textArea, color);
    }

    public void DrawImage(Graphics graphics, string directoryFullName, string imagePath, string imageFileName, float imgSizeX, float imgSizeY, float imgPosX, float imgPosY, ColorNode color)
    {        
        graphics.InitImage(imageFileName, Path.Combine(directoryFullName, imagePath));

        IntPtr imageId = graphics.GetTextureId(imageFileName);

        Vector2 position = new Vector2(imgPosX, imgPosY);
        Vector2 size = new Vector2(imgSizeX, imgSizeY);
    
        Vector2 topLeft = position;
        Vector2 topRight = position + new Vector2(size.X, 0);
        Vector2 bottomRight = position + size;
        Vector2 bottomLeft = position + new Vector2(0, size.Y);
    
        graphics.DrawQuad(imageId, topLeft, topRight, bottomRight, bottomLeft, color);
    }
}
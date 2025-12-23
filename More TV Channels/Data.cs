using Microsoft.Xna.Framework;

namespace MoreTVChannels
{
    /// <summary>
    /// Represents a custom TV channel for content packs.
    /// </summary>
    public class CustomChannelData
    {
        public string Name { get; set; } = "";
        public string Displayname { get; set; } = "";
        public List<string> Dialogues { get; set; } = new();
        public string? Texture { get; set; }
        public Point SpriteIndex { get; set; } = new Point(0, 0);
        public float AnimationInterval { get; set; } = 150f;
        public int AnimationLength { get; set; } = 2;
        public bool Flicker { get; set; } = false;
        public bool Flipped { get; set; } = false;
        public float AlphaFade { get; set; } = 0f;
        public Color Color { get; set; } = Color.White;
        public float Scale { get; set; } = 1f;
        public float ScaleChange { get; set; } = 0f;
        public bool HideFromMenu { get; set; } = false;
        public string? NextChannel { get; set; } = null;
        public List<string>? Actions { get; set; }
        public List<string>? Overlays { get; set; } = null;
    }

    public class OverlayData
    {
        public string Name { get; set; } = "";
        public string? Texture { get; set; }
        public Rectangle SpriteRegion { get; set; } = new Rectangle(0, 0, 16, 16);
        public float AnimationInterval { get; set; } = 150f;
        public int AnimationLength { get; set; } = 1;
        public bool Flicker { get; set; } = false;
        public bool Flipped { get; set; } = false;
        public float AlphaFade { get; set; } = 0f;
        public Color Color { get; set; } = Color.White;
        public float Scale { get; set; } = 1f;
        public float ScaleChange { get; set; } = 0f;
        public float Rotation { get; set; } = 0f;
        public float RotationChange { get; set; } = 0f;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float LayerDepth { get; set; } = 1f;
    }

    /// <summary>
    /// Represents an edit/override for a built-in TV channel.
    /// </summary>
    public class EditChannelData
    {
        public string? Displayname { get; set; } = null;
        public List<string>? Dialogues { get; set; } = null;
        public string? Texture { get; set; } = null;
        public Point? SpriteIndex { get; set; } = null;
        public float? AnimationInterval { get; set; } = null;
        public int? AnimationLength { get; set; } = null;
        public bool? Flicker { get; set; } = null;
        public bool? Flipped { get; set; } = null;
        public float? AlphaFade { get; set; } = null;
        public Color? Color { get; set; } = null;
        public float? Scale { get; set; } = null;
        public float? ScaleChange { get; set; } = null;
        public string? NextChannel { get; set; } = null;
        public List<string>? Actions { get; set; } = null;
        public List<string>? Overlays { get; set; } = null;
        public bool? HideFromMenu { get; set; } = null;
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoreTVChannels
{
    public static class ChannelPlayer
    {
        // Cache reflection fields
        private static readonly FieldInfo ScreenField = typeof(TV).GetField("screen", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ChannelField = typeof(TV).GetField("currentChannel", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Handles the selection of custom channels.
        /// </summary>
        public static void SelectCustomChannel(TV tv, Farmer who, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer) || answer == "(Leave)")
                return;

            // Try to get custom channel first
            var channel = AssetHandler<CustomChannelData>.Data.GetValueOrDefault(answer);

            // If custom channel found, play it
            if (channel != null)
            {
                PlayChannel(tv, channel);
            }
            // If not a custom channel, let vanilla logic handle it
        }

        /// <summary>
        /// Plays a custom channel
        /// </summary>
        public static void PlayChannel(TV tv, CustomChannelData baseChannel, EditChannelData? editData = null)
        {
            // Apply edits if present
            var channel = editData == null ? baseChannel : new CustomChannelData
            {
                Name = baseChannel.Name,
                Displayname = editData.Displayname ?? baseChannel.Displayname,
                Dialogues = editData.Dialogues ?? baseChannel.Dialogues,
                Texture = editData.Texture ?? baseChannel.Texture,
                SpriteIndex = editData.SpriteIndex ?? baseChannel.SpriteIndex,
                AnimationInterval = editData.AnimationInterval ?? baseChannel.AnimationInterval,
                AnimationLength = editData.AnimationLength ?? baseChannel.AnimationLength,
                Flicker = editData.Flicker ?? baseChannel.Flicker,
                Flipped = editData.Flipped ?? baseChannel.Flipped,
                AlphaFade = editData.AlphaFade ?? baseChannel.AlphaFade,
                Color = editData.Color ?? baseChannel.Color,
                Scale = editData.Scale ?? baseChannel.Scale,
                ScaleChange = editData.ScaleChange ?? baseChannel.ScaleChange,
                HideFromMenu = editData.HideFromMenu ?? baseChannel.HideFromMenu,
                NextChannel = editData.NextChannel ?? baseChannel.NextChannel,
                Actions = editData.Actions ?? baseChannel.Actions,
                Overlays = editData.Overlays ?? baseChannel.Overlays
            };

            // Notify API
            (ModEntry.Instance?.GetApi() as Api)?.InvokeOnChannelStarted(channel.Name);

            // Setup TV state
            OverlayManager.ClearOverlays();
            ChannelField.SetValue(tv, GetVanillaChannelId(channel.Name));

            // Create and set screen sprite
            var sprite = new TemporaryAnimatedSprite(
                textureName: null,
                new Rectangle(channel.SpriteIndex.X, channel.SpriteIndex.Y, 42, 28),
                channel.AnimationInterval, channel.AnimationLength, 999999,
                tv.getScreenPosition(), channel.Flicker, channel.Flipped,
                (float)(tv.boundingBox.Bottom - 1) / 10000f + 1E-05f,
                channel.AlphaFade, channel.Color,
                channel.Scale * tv.getScreenSizeModifier(),
                channel.ScaleChange, 0f, 0f);

            // Load texture if specified
            if (channel.Texture != null)
            {
                try
                {
                    sprite.texture = Game1.content.Load<Texture2D>(channel.Texture);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor?.Log($"Failed to load texture {channel.Texture}: {ex.Message}", LogLevel.Error);
                }
            }

            ScreenField.SetValue(tv, sprite);

            // Show overlays
            OverlayManager.ShowOverlays(channel.Overlays, tv.getScreenPosition(), tv.getScreenSizeModifier(), tv);

            // Display dialogues
            if (channel.Dialogues?.Count > 0)
                Game1.multipleDialogues(channel.Dialogues.ToArray());
            else
                Game1.drawObjectDialogue("...");

            // Setup after-dialogue behavior
            if (!string.IsNullOrEmpty(channel.NextChannel))
            {
                var next = channel.NextChannel; // Capture for closure
                Game1.afterDialogues = () =>
                {
                    Actions.RunChannelActions(channel);
                    OverlayManager.ClearOverlays();

                    // Chain to next channel
                    var nextChannel = AssetHandler<CustomChannelData>.Data.GetValueOrDefault(next);

                    if (nextChannel != null)
                        PlayChannel(tv, nextChannel);
                    else
                        tv.turnOffTV();
                };
            }
            else
            {
                Game1.afterDialogues = () =>
                {
                    Actions.RunChannelActions(channel);
                    OverlayManager.ClearOverlays();
                    tv.turnOffTV();
                };
            }
        }

        /// <summary>
        /// Gets the vanilla channel ID for proper cleanup.
        /// </summary>
        private static int GetVanillaChannelId(string name) => name switch
        {
            "Weather" => 2,
            "Fortune" => 3,
            "Livin'" => 4,
            "The" => 5,
            "???" => 666,
            "Fishing" => 6,
            _ => -1
        };
    }
}

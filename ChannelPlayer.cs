using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ModEntry.ModMonitor?.Log($"[SelectCustomChannel] Called with answer: {answer}", LogLevel.Debug);
            if (string.IsNullOrWhiteSpace(answer) || answer == "(Leave)")
            {
                ModEntry.ModMonitor?.Log($"[SelectCustomChannel] Empty answer or Leave, exiting", LogLevel.Debug);
                return;
            }

            // Try to get custom channel first
            var channel = ModEntry.CustomChannels.Data.GetValueOrDefault(answer);

            // If custom channel found, check for BQuestions
            if (channel != null)
            {
                ModEntry.ModMonitor?.Log($"[SelectCustomChannel] Found custom channel: {channel.Name}", LogLevel.Debug);

                // Check if channel has BQuestions
                if (channel.BQuestions != null && channel.BQuestions.Answers.Count > 0)
                {
                    ModEntry.ModMonitor?.Log($"[SelectCustomChannel] Channel has BQuestions, showing question dialog", LogLevel.Debug);
                    QuestionsManager.ShowBQuestions(tv, channel);
                }
                else
                {
                    ModEntry.ModMonitor?.Log($"[SelectCustomChannel] No BQuestions, playing channel directly", LogLevel.Debug);
                    PlayChannel(tv, channel);
                }
            }
            else
            {
                ModEntry.ModMonitor?.Log($"[SelectCustomChannel] No custom channel found for: {answer}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Plays a custom channel
        /// </summary>
        public static void PlayChannel(TV tv, CustomChannelData baseChannel, EditChannelData? editData = null)
        {
            ModEntry.ModMonitor?.Log($"[PlayChannel] Starting for channel: {baseChannel.Name}", LogLevel.Debug);

            // Apply edits if present
            var channel = editData == null ? baseChannel : new CustomChannelData
            {
                Name = baseChannel.Name,
                Displayname = editData.Displayname ?? baseChannel.Displayname,
                Dialogues = editData.Dialogues ?? baseChannel.Dialogues,
                Texture = editData.Texture ?? baseChannel.Texture,
                SpriteRegion = editData.SpriteRegion ?? baseChannel.SpriteRegion,
                AnimationInterval = editData.AnimationInterval ?? baseChannel.AnimationInterval,
                AnimationLength = editData.AnimationLength ?? baseChannel.AnimationLength,
                Flicker = editData.Flicker ?? baseChannel.Flicker,
                Flipped = editData.Flipped ?? baseChannel.Flipped,
                AlphaFade = editData.AlphaFade ?? baseChannel.AlphaFade,
                Color = editData.Color ?? baseChannel.Color,
                Scale = editData.Scale ?? baseChannel.Scale,
                ScaleChange = editData.ScaleChange ?? baseChannel.ScaleChange,
                LayerDepth = editData.LayerDepth ?? baseChannel.LayerDepth,
                HideFromMenu = editData.HideFromMenu ?? baseChannel.HideFromMenu,
                NextChannel = editData.NextChannel ?? baseChannel.NextChannel,
                Actions = editData.Actions ?? baseChannel.Actions,
                Overlays = editData.Overlays ?? baseChannel.Overlays,
                BQuestions = editData.BQuestions ?? baseChannel.BQuestions,
                EQuestions = editData.EQuestions ?? baseChannel.EQuestions
            };

            ModEntry.ModMonitor?.Log($"[PlayChannel] Channel data prepared", LogLevel.Debug);

            // Notify API
            (ModEntry.Instance?.GetApi() as Api)?.InvokeOnChannelStarted(channel.Name);
            ModEntry.ModMonitor?.Log($"[PlayChannel] API notified", LogLevel.Debug);

            // Setup TV state
            OverlayManager.ClearOverlays();
            ChannelField.SetValue(tv, GetVanillaChannelId(channel.Name));
            ModEntry.ModMonitor?.Log($"[PlayChannel] TV state setup complete", LogLevel.Debug);

            // Create and set screen sprite
            // Calculate base depth - use FF's depth if available, otherwise vanilla
            float baseDepth;
            if (ModEntry.FurnitureFrameworkAPI?.TryGetScreenDepth(tv, out float? ffDepth, overlay: false) == true && ffDepth.HasValue)
            {
                baseDepth = ffDepth.Value;
                ModEntry.ModMonitor?.Log($"[PlayChannel] Using FF depth: {baseDepth}", LogLevel.Debug);
            }
            else
            {
                baseDepth = (float)(tv.boundingBox.Bottom - 1) / 10000f;
                ModEntry.ModMonitor?.Log($"[PlayChannel] Using vanilla depth: {baseDepth}", LogLevel.Debug);
            }

            var sprite = new TemporaryAnimatedSprite(
                textureName: null,
                channel.SpriteRegion,
                channel.AnimationInterval, channel.AnimationLength, 999999,
                tv.getScreenPosition(), channel.Flicker, channel.Flipped,
                baseDepth + channel.LayerDepth * 1E-05f,
                channel.AlphaFade, channel.Color,
                channel.Scale * tv.getScreenSizeModifier(),
                channel.ScaleChange, 0f, 0f);

            // Load texture if specified
            if (channel.Texture != null)
            {
                try
                {
                    sprite.texture = Game1.content.Load<Texture2D>(channel.Texture);
                    ModEntry.ModMonitor?.Log($"[PlayChannel] Texture loaded: {channel.Texture}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor?.Log($"[PlayChannel] Failed to load texture {channel.Texture}: {ex.Message}", LogLevel.Error);
                }
            }

            ScreenField.SetValue(tv, sprite);
            ModEntry.ModMonitor?.Log($"[PlayChannel] Screen sprite set", LogLevel.Debug);

            // Show overlays
            OverlayManager.ShowOverlays(channel.Overlays, tv.getScreenPosition(), tv.getScreenSizeModifier(), tv);
            ModEntry.ModMonitor?.Log($"[PlayChannel] Overlays shown", LogLevel.Debug);

            // Display dialogues
            if (channel.Dialogues?.Count > 0)
            {
                ModEntry.ModMonitor?.Log($"[PlayChannel] Showing {channel.Dialogues.Count} dialogues", LogLevel.Debug);
                Game1.multipleDialogues(channel.Dialogues.ToArray());
            }
            else
            {
                ModEntry.ModMonitor?.Log($"[PlayChannel] No dialogues, showing default", LogLevel.Debug);
                Game1.drawObjectDialogue("...");
            }

            // Setup after-dialogue behavior
            Game1.afterDialogues = () =>
            {
                ModEntry.ModMonitor?.Log($"[PlayChannel] After dialogues - running actions", LogLevel.Debug);
                Actions.RunChannelActions(channel);
                OverlayManager.ClearOverlays();

                // Check for EQuestions
                if (channel.EQuestions != null && channel.EQuestions.Answers.Count > 0)
                {
                    ModEntry.ModMonitor?.Log($"[PlayChannel] Channel has EQuestions, showing them", LogLevel.Debug);
                    QuestionsManager.ShowEQuestions(tv, channel, channel.NextChannel);
                }
                else if (!string.IsNullOrEmpty(channel.NextChannel))
                {
                    var next = channel.NextChannel;
                    ModEntry.ModMonitor?.Log($"[PlayChannel] No EQuestions, chaining to: {next}", LogLevel.Debug);

                    // Chain to next channel
                    var nextChannel = ModEntry.CustomChannels.Data.GetValueOrDefault(next);
                    if (nextChannel != null)
                    {
                        ModEntry.ModMonitor?.Log($"[PlayChannel] Found next channel, playing it", LogLevel.Debug);
                        PlayChannel(tv, nextChannel);
                    }
                    else
                    {
                        ModEntry.ModMonitor?.Log($"[PlayChannel] Next channel not found, turning off TV", LogLevel.Debug);
                        tv.turnOffTV();
                    }
                }
                else
                {
                    ModEntry.ModMonitor?.Log($"[PlayChannel] No next channel or EQuestions, turning off TV", LogLevel.Debug);
                    tv.turnOffTV();
                }
            };

            ModEntry.ModMonitor?.Log($"[PlayChannel] Channel setup complete", LogLevel.Debug);
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

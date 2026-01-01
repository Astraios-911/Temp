using StardewValley;
using StardewValley.Triggers;

namespace MoreTVChannels
{
    internal static class Actions
    {
        /// <summary>
        /// Executes the actions defined for a custom or edited channel.
        /// </summary>
        public static void RunChannelActions(object channel)
        {
            // Get actions from either custom or edit channel
            var actions = channel switch
            {
                CustomChannelData custom => custom.Actions,
                EditChannelData edit => edit.Actions,
                _ => null
            };

            if (actions == null) return;

            var triggerActions = Game1.content.Load<List<StardewValley.GameData.TriggerActionData>>("Data/TriggerActions")
                .ToDictionary(a => a.Id);

            foreach (var actionOrId in actions)
            {
                bool ran = false;
                string error = null;

                // Try to run as trigger action ID first
                if (triggerActions.TryGetValue(actionOrId, out var triggerData))
                {
                    // Find cached action from triggers
                    var triggers = triggerData.Trigger?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    var cached = triggers
                        .SelectMany(t => TriggerActionManager.GetActionsForTrigger(t))
                        .FirstOrDefault(a => a.Data.Id == actionOrId);

                    if (cached != null)
                        ran = TriggerActionManager.TryRunActions(cached, TriggerActionManager.trigger_manual);
                    else
                        error = $"Could not find trigger action for ID '{actionOrId}'.";
                }
                else
                {
                    // Try to run as raw action string
                    ran = TriggerActionManager.TryRunAction(actionOrId, out error, out var exception);
                    error ??= exception?.Message;
                }

                if (!ran)
                    ModEntry.ModMonitor?.Log($"Failed to run action '{actionOrId}': {error}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
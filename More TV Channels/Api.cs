namespace MoreTVChannels
{
    /// <summary>Defines the API for More TV Channels.</summary>
    public interface IMoreTVChannelsAPI
    {
        /// <summary>Raised when a custom or edited TV channel starts playing.</summary>
        event Action<string> OnChannelStarted;

        /// <summary>Raised when the TV is turned off after a channel finishes.</summary>
        event Action OnTVTurnedOff;

        /// <summary>Gets the internal name of the channel that is currently playing.</summary>
        string? GetCurrentChannel();

        /// <summary>Register a new custom TV channel at runtime.</summary>
        void RegisterCustomChannel(CustomChannelData channel);

        /// <summary>Edit or override a built-in TV channel at runtime.</summary>
        void EditChannel(string name, EditChannelData edit);

        /// <summary>Register a new overlay sprite at runtime.</summary>
        void RegisterOverlay(OverlayData overlay);
    }

    /// <summary>Implements the IMoreTVChannelsAPI interface.</summary>
    public class Api : IMoreTVChannelsAPI
    {
        public event Action<string>? OnChannelStarted;
        public event Action? OnTVTurnedOff;

        private string? _currentChannel;

        public string? GetCurrentChannel() => _currentChannel;

        /// <summary>Invokes the OnChannelStarted event and sets the current channel.</summary>
        internal void InvokeOnChannelStarted(string channelName)
        {
            _currentChannel = channelName;
            OnChannelStarted?.Invoke(channelName);
        }

        /// <summary>Invokes the OnTVTurnedOff event and clears the current channel.</summary>
        internal void InvokeOnTVTurnedOff()
        {
            _currentChannel = null;
            OnTVTurnedOff?.Invoke();
        }

        public void RegisterCustomChannel(CustomChannelData channel)
            => AssetHandler<CustomChannelData>.Data[channel.Name] = channel;

        public void EditChannel(string name, EditChannelData edit)
            => AssetHandler<EditChannelData>.Data[name] = edit;

        public void RegisterOverlay(OverlayData overlay)
            => AssetHandler<OverlayData>.Data[overlay.Name] = overlay;
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Audio;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace App1
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Windows.Devices.Enumeration.DeviceInformation> devices = new ObservableCollection<Windows.Devices.Enumeration.DeviceInformation>();
        private Dictionary<String, AudioPlaybackConnection> audioPlaybackConnections;

        private DeviceWatcher deviceWatcher;

        public MainPage()
        {
            this.InitializeComponent();
            
        }


        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            audioPlaybackConnections = new Dictionary<string, AudioPlaybackConnection>();

            // Start watching for paired Bluetooth devices. 
            this.deviceWatcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());

            // Register event handlers before starting the watcher. 
            this.deviceWatcher.Added += this.DeviceWatcher_Added;
            this.deviceWatcher.Removed += this.DeviceWatcher_Removed;

            this.deviceWatcher.Start();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // Collections bound to the UI are updated in the UI thread. 
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.devices.Add(deviceInfo);
            });
        }

        private async void EnableAudioPlaybackConnectionButton_Click(object sender, RoutedEventArgs e)
        {

            if (!(DeviceListView.SelectedItem is null))
            {
                var selectedDeviceId = (DeviceListView.SelectedItem as DeviceInformation).Id;
                if (!this.audioPlaybackConnections.ContainsKey(selectedDeviceId))
                {
                    // Create the audio playback connection from the selected device id and add it to the dictionary. 
                    // This will result in allowing incoming connections from the remote device. 
                    var playbackConnection = AudioPlaybackConnection.TryCreateFromId(selectedDeviceId);

                    if (playbackConnection != null)
                    {
                        // The device has an available audio playback connection. 
                        playbackConnection.StateChanged += this.AudioPlaybackConnection_ConnectionStateChanged;
                        this.audioPlaybackConnections.Add(selectedDeviceId, playbackConnection);
                        await playbackConnection.StartAsync();
                        OpenAudioPlaybackConnectionButtonButton.IsEnabled = true;
                    }
                }
            }
        }

        private async void OpenAudioPlaybackConnectionButtonButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDevice = (DeviceListView.SelectedItem as DeviceInformation).Id;
            AudioPlaybackConnection selectedConnection;

            if (this.audioPlaybackConnections.TryGetValue(selectedDevice, out selectedConnection))
            {
                if ((await selectedConnection.OpenAsync()).Status == AudioPlaybackConnectionOpenResultStatus.Success)
                {
                    // Notify that the AudioPlaybackConnection is connected. 
                    ConnectionState.Text = "Connected";
                }
                else
                {
                    // Notify that the connection attempt did not succeed. 
                    ConnectionState.Text = "Disconnected (attempt failed)";
                }
            }
        }

        private async void AudioPlaybackConnection_ConnectionStateChanged(AudioPlaybackConnection sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (sender.State == AudioPlaybackConnectionState.Closed)
                {
                    ConnectionState.Text = "Disconnected";
                }
                else if (sender.State == AudioPlaybackConnectionState.Opened)
                {
                    ConnectionState.Text = "Connected";
                }
                else
                {
                    ConnectionState.Text = "Unknown";
                }
            });
        }

        private void ReleaseAudioPlaybackConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if an audio playback connection was already created for the selected device Id. If it was then release its reference to deactivate it. 
            // The underlying transport is deactivated when all references are released. 
            if (!(DeviceListView.SelectedItem is null))
            {
                var selectedDeviceId = (DeviceListView.SelectedItem as DeviceInformation).Id;
                if (audioPlaybackConnections.ContainsKey(selectedDeviceId))
                {
                    AudioPlaybackConnection connectionToRemove = audioPlaybackConnections[selectedDeviceId];
                    connectionToRemove.Dispose();
                    this.audioPlaybackConnections.Remove(selectedDeviceId);

                    // Notify that the media device has been deactivated. 
                    ConnectionState.Text = "Disconnected";
                    OpenAudioPlaybackConnectionButtonButton.IsEnabled = false;
                }
            }
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // Collections bound to the UI are updated in the UI thread. 
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Find the device for the given id and remove it from the list. 
                foreach (DeviceInformation device in this.devices)
                {
                    if (device.Id == deviceInfoUpdate.Id)
                    {
                        this.devices.Remove(device);
                        break;
                    }
                }

                if (audioPlaybackConnections.ContainsKey(deviceInfoUpdate.Id))
                {
                    AudioPlaybackConnection connectionToRemove = audioPlaybackConnections[deviceInfoUpdate.Id];
                    connectionToRemove.Dispose();
                    this.audioPlaybackConnections.Remove(deviceInfoUpdate.Id);
                }
            });
        }

    }
}

using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LoudnessMeterUI.DataModels;
using LoudnessMeterUI.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using DynamicData;

namespace LoudnessMeterUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        #region Private members

        /// <summary>
        /// The audio capture service
        /// </summary>
        private IAudioCaptureService mAudioCaptureService;

        /// <summary>
        /// A slow tick counter to update the text slower then the graph and bars
        /// </summary>
        private int mUpdateCounter;

        #endregion

        #region Public Properties

        [ObservableProperty]
        private string _boldTitle = "AVALONIA";

        [ObservableProperty]
        private string _regularTitle = "LOUDNESS METER";

        [ObservableProperty]
        private string _shortTermLoudness = "0 LUFS";

        [ObservableProperty]
        private string _integratedLoudness = "0 LUFS";

        [ObservableProperty]
        private string _loudnessRange = "0 LU";

        [ObservableProperty]
        private string _realtimeDynamics = "0 LU";

        [ObservableProperty]
        private string _averageDynamics = "0 LU";

        [ObservableProperty]
        private string _momentaryMaxLoudness = "0 LUFS";

        [ObservableProperty]
        private string _shortTermMAxLoudness = "0 LUFS";

        [ObservableProperty]
        private string _truePeakMax = "0 dB";

        [ObservableProperty]
        private bool _channelConfigurationListIsOpen;

        [ObservableProperty]
        private double _volumePercentPosition;

        [ObservableProperty]
        private double _volumeContainerHeight;

        [ObservableProperty]
        private double _volumeBarHeight;

        [ObservableProperty]
        private double _volumeBarMaskHeight;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ChannelConfigurationItem> _channelConfigurations = default!;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChannelConfigurationButtonText))]
        private ChannelConfigurationItem? _selectedChannelConfiguration;

        public List<Axis> YAxis { get; set; } =
            new List<Axis>()
            {
                new Axis
                {
                    MinStep = 10,
                    ForceStepToMin = true,
                    MinLimit = 0,
                    MaxLimit = 60,
                    Labeler = (val) => (Math.Min(60, Math.Max(0, val)) - 60).ToString(),
                    IsVisible = false,
                    //IsInverted = true
                }
            };

        public string ChannelConfigurationButtonText => SelectedChannelConfiguration?.ShortText ?? "Select Channel";

        public ObservableCollection<ObservableValue> MainChartValues = new ObservableCollection<ObservableValue>();

        public ISeries[] Series { get; set; }

        #endregion

        #region Public Commands

        [RelayCommand]
        private void ChannelConfigurationButtonPressed() => ChannelConfigurationListIsOpen ^= true;

        [RelayCommand]
        private void ChannelConfigurationItemPressed(ChannelConfigurationItem item)
        {
            //  Update the selected item
            SelectedChannelConfiguration = item;

            //  Close the menu
            ChannelConfigurationListIsOpen = false;
        }

        /// <summary>
        /// Do initial loading of data and settings up service
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task LoadAsync()
        {
            //  Get the channel configuration data
            var channelConfigurations = await mAudioCaptureService.GetChannelConfigurationsAsync();

            //  Create a grouping from the flat data
            ChannelConfigurations = new ObservableGroupedCollection<string, ChannelConfigurationItem>(
                channelConfigurations.GroupBy(item => item.Group));

            StartCapture(deviceId: 0);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="audioCaptureService">The audio interface service</param>
        public MainViewModel(IAudioCaptureService audioCaptureService)
        {
            mAudioCaptureService = audioCaptureService;

            Initialize();
        }
        /// <summary>
        /// Design-time constructor
        /// </summary>
        public MainViewModel()
        {
            mAudioCaptureService = new BassAudioCaptureService();

            Initialize();
        }

        #endregion

        private void Initialize()
        {
            MainChartValues.AddRange(Enumerable.Range(0,200).Select(f => new ObservableValue(0)));

            Series = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = MainChartValues,
                    GeometrySize = 0,
                    GeometryStroke = null,
                    Fill = new SolidColorPaint(new SKColor(63,77,99)),
                    Stroke = new SolidColorPaint(new SKColor(120,152,203))  { StrokeThickness = 3 }
                }
            };
        }

        /// <summary>
        /// Starts capturing audio from the specified device
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        private void StartCapture(int deviceId)
        {
            //  Initialize capturing on specific device
            mAudioCaptureService.InitCapture(deviceId);

            //  Listen out for chunks of information
            mAudioCaptureService.AudioChunkAvailable += audioChunkData =>
            {
                ProcessAudioChunk(audioChunkData);
            };

            //  Start capturing
            mAudioCaptureService.Start();
        }

        private void ProcessAudioChunk(AudioChunkData audioChunkData)
        {
            //  Counter between 0-1-2
            mUpdateCounter = (mUpdateCounter + 1) % 3;

            //  Every time counter is at 0...
            if(mUpdateCounter == 0)
            {
                ShortTermLoudness = $"{Math.Max(-60, audioChunkData.ShortTermLUFS):0.0} LUFS";
                IntegratedLoudness = $"{Math.Max(-60, audioChunkData.IntegratedLUFS):0.0} LUFS";
                LoudnessRange = $"{Math.Max(-60, audioChunkData.LoudnessRange):0.0} LU";
                RealtimeDynamics = $"{Math.Max(-60, audioChunkData.RealtimeDynamics):0.0} LU";
                AverageDynamics = $"{Math.Max(-60, audioChunkData.AverageRealtimeDynamics):0.0} LU";
                MomentaryMaxLoudness = $"{Math.Max(-60, audioChunkData.MomentaryMaxLUFS):0.0} LUFS";
                ShortTermMAxLoudness = $"{Math.Max(-60, audioChunkData.ShortTermMaxLUFS):0.0} LUFS";
                TruePeakMax = $"{Math.Max(-60, audioChunkData.TruePeakMax):0.0} dB";

                Dispatcher.UIThread.Invoke(() =>
                {
                    MainChartValues.RemoveAt(0);
                    MainChartValues.Add(new(Math.Max(0,60 + audioChunkData.ShortTermLUFS)));
                });
            }

            //  Set volume bar height
            VolumeBarMaskHeight = Math.Min(VolumeBarHeight, VolumeBarHeight / 60 * -audioChunkData.Loudness);

            //  Set volume arrow height
            VolumePercentPosition = Math.Min(VolumeContainerHeight, VolumeContainerHeight / 60 * -audioChunkData.ShortTermLUFS);
        }
    }
}

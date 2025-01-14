﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Xaml.Media.Imaging;

namespace VRCHapticsLite
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private IDirect3DDevice _device;
        private CaptureEngine _capture;
        private HapticsPlayer _player;
        private RotorPlayer _rotor_player;

        private HapticsBridge _headBridge;
        private HapticsBridge _vestBridge;
        private HapticsBridge _leftArmBridge;
        private HapticsBridge _rightArmBridge;
        private RotorBridge _rotorBridge;

        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            this.DataContext = vm;
            LoadSettings();

            _device = Direct3D11Helper.CreateDevice();
            _player = new HapticsPlayer();
            _rotor_player = new RotorPlayer();

            _headBridge = new HapticsBridge(
                new HapticsBridgeParameters(
                    6, 1, new HapticsPointSet[]
                    {
                        new HapticsPointSet(HapticsPosition.Head, new Point[]
                        {
                            new Point(0, 0), new Point(1, 0), new Point(2, 0),
                            new Point(3, 0), new Point(4, 0), new Point(5, 0),
                        })
                    }),
                vm.Head,
                vm.ActiveColor,
                vm.InactiveColor,
                _player);
            _vestBridge = new HapticsBridge(
                new HapticsBridgeParameters(
                    8, 5, new HapticsPointSet[]
                    {
                        new HapticsPointSet(HapticsPosition.VestFront, new Point[]
                        {
                            new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0),
                            new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1),
                            new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2),
                            new Point(0, 3), new Point(1, 3), new Point(2, 3), new Point(3, 3),
                            new Point(0, 4), new Point(1, 4), new Point(2, 4), new Point(3, 4),
                        }),
                        new HapticsPointSet(HapticsPosition.VestBack, new Point[]
                        {
                            new Point(4, 0), new Point(5, 0), new Point(6, 0), new Point(7, 0),
                            new Point(4, 1), new Point(5, 1), new Point(6, 1), new Point(7, 1),
                            new Point(4, 2), new Point(5, 2), new Point(6, 2), new Point(7, 2),
                            new Point(4, 3), new Point(5, 3), new Point(6, 3), new Point(7, 3),
                            new Point(4, 4), new Point(5, 4), new Point(6, 4), new Point(7, 4),
                        }),
                    }),
                vm.Vest,
                vm.ActiveColor,
                vm.InactiveColor,
                _player);
            _leftArmBridge = new HapticsBridge(
                new HapticsBridgeParameters(
                    5, 4, new HapticsPointSet[]
                    {
                        new HapticsPointSet(HapticsPosition.Left, new Point[]
                        {
                            new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0), new Point(4, 0),
                            new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1), new Point(4, 1),
                            new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2), new Point(4, 2),
                            new Point(0, 3), new Point(1, 3), new Point(2, 3), new Point(3, 3), new Point(4, 3),
                        })
                    }),
                vm.LeftArm,
                vm.ActiveColor,
                vm.InactiveColor,
                _player);
            _rightArmBridge = new HapticsBridge(
                new HapticsBridgeParameters(
                    5, 4, new HapticsPointSet[]
                    {
                        new HapticsPointSet(HapticsPosition.Right, new Point[]
                        {
                            new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0), new Point(4, 0),
                            new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1), new Point(4, 1),
                            new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2), new Point(4, 2),
                            new Point(0, 3), new Point(1, 3), new Point(2, 3), new Point(3, 3), new Point(4, 3),
                        })
                    }),
                vm.RightArm,
                vm.ActiveColor,
                vm.InactiveColor,
                _player);
            _rotorBridge = new RotorBridge(
                new RotorBridgeParameters(
                    1, 2, new RotorPointSet[]
                    {
                        new RotorPointSet(RotorPosition.MainPosition, new Point[]
                        {
                            new Point(0, 0),
                            new Point(0, 1),
                        })
                    }),
                vm.Rotor,
                vm.ActiveColor,
                vm.InactiveColor,
                _rotor_player);

            vm.Rotor.Enabled.Subscribe(state => { if (!state) { _rotorBridge.OnDisabled(); } });
        }

        private void LoadSettings()
        {
            var settings = Properties.Settings.Default;
            var dc = this.DataContext as MainViewModel;
            dc.Head.Enabled.Value = settings.Head_Enabled;
            dc.Head.Power.Value = settings.Head_Power;
            dc.Head.X.Value = settings.Head_X;
            dc.Head.Y.Value = settings.Head_Y;
            dc.Head.Width.Value = settings.Head_Width;
            dc.Head.Height.Value = settings.Head_Height;
            dc.Vest.Enabled.Value = settings.Vest_Enabled;
            dc.Vest.Power.Value = settings.Vest_Power;
            dc.Vest.X.Value = settings.Vest_X;
            dc.Vest.Y.Value = settings.Vest_Y;
            dc.Vest.Width.Value = settings.Vest_Width;
            dc.Vest.Height.Value = settings.Vest_Height;
            dc.LeftArm.Enabled.Value = settings.LeftArm_Enabled;
            dc.LeftArm.Power.Value = settings.LeftArm_Power;
            dc.LeftArm.X.Value = settings.LeftArm_X;
            dc.LeftArm.Y.Value = settings.LeftArm_Y;
            dc.LeftArm.Width.Value = settings.LeftArm_Width;
            dc.LeftArm.Height.Value = settings.LeftArm_Height;
            dc.RightArm.Enabled.Value = settings.RightArm_Enabled;
            dc.RightArm.Power.Value = settings.RightArm_Power;
            dc.RightArm.X.Value = settings.RightArm_X;
            dc.RightArm.Y.Value = settings.RightArm_Y;
            dc.RightArm.Width.Value = settings.RightArm_Width;
            dc.RightArm.Height.Value = settings.RightArm_Height;
            dc.Rotor.Enabled.Value = settings.Rotor_Enabled;
            dc.Rotor.Power.Value = settings.Rotor_Power;
            dc.Rotor.X.Value = settings.Rotor_X;
            dc.Rotor.Y.Value = settings.Rotor_Y;
            dc.Rotor.Width.Value = settings.Rotor_Width;
            dc.Rotor.Height.Value = settings.Rotor_Height;
            dc.ActiveColor.MinR.Value = settings.Active_MinR;
            dc.ActiveColor.MaxR.Value = settings.Active_MaxR;
            dc.ActiveColor.MinG.Value = settings.Active_MinG;
            dc.ActiveColor.MaxG.Value = settings.Active_MaxG;
            dc.ActiveColor.MinB.Value = settings.Active_MinB;
            dc.ActiveColor.MaxB.Value = settings.Active_MaxB;
            dc.InactiveColor.MinR.Value = settings.Inactive_MinR;
            dc.InactiveColor.MaxR.Value = settings.Inactive_MaxR;
            dc.InactiveColor.MinG.Value = settings.Inactive_MinG;
            dc.InactiveColor.MaxG.Value = settings.Inactive_MaxG;
            dc.InactiveColor.MinB.Value = settings.Inactive_MinB;
            dc.InactiveColor.MaxB.Value = settings.Inactive_MaxB;
        }

        private void StoreSettings()
        {
            var settings = Properties.Settings.Default;
            var dc = this.DataContext as MainViewModel;
            settings.Head_Enabled = dc.Head.Enabled.Value;
            settings.Head_Power = dc.Head.Power.Value;
            settings.Head_X = dc.Head.X.Value;
            settings.Head_Y = dc.Head.Y.Value;
            settings.Head_Width = dc.Head.Width.Value;
            settings.Head_Height = dc.Head.Height.Value;
            settings.Vest_Enabled = dc.Vest.Enabled.Value;
            settings.Vest_Power = dc.Vest.Power.Value;
            settings.Vest_X = dc.Vest.X.Value;
            settings.Vest_Y = dc.Vest.Y.Value;
            settings.Vest_Width = dc.Vest.Width.Value;
            settings.Vest_Height = dc.Vest.Height.Value;
            settings.LeftArm_Enabled = dc.LeftArm.Enabled.Value;
            settings.LeftArm_Power = dc.LeftArm.Power.Value;
            settings.LeftArm_X = dc.LeftArm.X.Value;
            settings.LeftArm_Y = dc.LeftArm.Y.Value;
            settings.LeftArm_Width = dc.LeftArm.Width.Value;
            settings.LeftArm_Height = dc.LeftArm.Height.Value;
            settings.RightArm_Enabled = dc.RightArm.Enabled.Value;
            settings.RightArm_Power = dc.RightArm.Power.Value;
            settings.RightArm_X = dc.RightArm.X.Value;
            settings.RightArm_Y = dc.RightArm.Y.Value;
            settings.RightArm_Width = dc.RightArm.Width.Value;
            settings.RightArm_Height = dc.RightArm.Height.Value;
            settings.Rotor_Enabled = dc.Rotor.Enabled.Value;
            settings.Rotor_Power = dc.Rotor.Power.Value;
            settings.Rotor_X = dc.Rotor.X.Value;
            settings.Rotor_Y = dc.Rotor.Y.Value;
            settings.Rotor_Width = dc.Rotor.Width.Value;
            settings.Rotor_Height = dc.Rotor.Height.Value;
            settings.Active_MinR = dc.ActiveColor.MinR.Value;
            settings.Active_MaxR = dc.ActiveColor.MaxR.Value;
            settings.Active_MinG = dc.ActiveColor.MinG.Value;
            settings.Active_MaxG = dc.ActiveColor.MaxG.Value;
            settings.Active_MinB = dc.ActiveColor.MinB.Value;
            settings.Active_MaxB = dc.ActiveColor.MaxB.Value;
            settings.Inactive_MinR = dc.InactiveColor.MinR.Value;
            settings.Inactive_MaxR = dc.InactiveColor.MaxR.Value;
            settings.Inactive_MinG = dc.InactiveColor.MinG.Value;
            settings.Inactive_MaxG = dc.InactiveColor.MaxG.Value;
            settings.Inactive_MinB = dc.InactiveColor.MinB.Value;
            settings.Inactive_MaxB = dc.InactiveColor.MaxB.Value;
            settings.Save();
        }

        private void StartCapture(GraphicsCaptureItem item)
        {
            _capture = new CaptureEngine(_device, item);
            _capture.FrameArrived += _headBridge.OnFrameArrived;
            _capture.FrameArrived += _vestBridge.OnFrameArrived;
            _capture.FrameArrived += _leftArmBridge.OnFrameArrived;
            _capture.FrameArrived += _rightArmBridge.OnFrameArrived;
            _capture.FrameArrived += _rotorBridge.OnFrameArrived;
            _capture.StartCapture();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            _hwnd = interopWindow.Handle;

            await Task.WhenAll(
                Task.Run(async () => { await _player.SetupAsync(); }),
                Task.Run(async () => { await _rotor_player.SetupAsync(); }));
            //await _player.SetupAsync();
            //await _rotor_player.SetupAsync();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StoreSettings();
        }

        private async void SelectWindowButton_Click(object sender, RoutedEventArgs e)
        {
            await StartPickerCaptureAsync();
        }

        private async Task StartPickerCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(_hwnd);
            var item = await picker.PickSingleItemAsync();
            if (item != null)
            {
                var dc = this.DataContext as MainViewModel;
                dc.TargetName.Value = item.DisplayName;
                StartCapture(item);
            }
        }
    }
}
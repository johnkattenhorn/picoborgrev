using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PicoBorgRev;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PicoBorgRevGui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly PicoBorgRev.PicoBorgRev _picoborgrev;

        public MainPage()
        {
            this.InitializeComponent();

            _picoborgrev = new PicoBorgRev.PicoBorgRev();
            _picoborgrev.ControllerMessageReceived += PicoborgrevOnControllerMessageReceived;

            Loaded += OnLoaded;
        }

        private void PicoborgrevOnControllerMessageReceived(object sender, ControllerMessageEventArgs e)
        {
            WriteToOutputTextBlock(e.Message);
        }

        private async void WriteToOutputTextBlock(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                outputTextBlock.Text = outputTextBlock.Text +
                                       ((outputTextBlock.Text == "")
                                           ? ""
                                           : (Environment.NewLine + Environment.NewLine)) + text;
                scrollViewer.UpdateLayout();
                scrollViewer.ChangeView(0, double.MaxValue, 1.0f);
            });
        }
        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            await _picoborgrev.InitializeAsync();
        }

        private void AllStopButton_OnClick(object sender, RoutedEventArgs e)
        {
            Motor1Slider.Value = 0;
            Motor2Slider.Value = 0;
        }
        
        private void Motor1Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _picoborgrev.SetMotor1(e.NewValue);
        }

        private void Motor2Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _picoborgrev.SetMotor2(e.NewValue);
        }
    }
}
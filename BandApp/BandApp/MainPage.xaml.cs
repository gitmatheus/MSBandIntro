using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BandApp
{
    using Microsoft.Band;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Client;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SensorData _data = new SensorData();
        public static HubConnection _conn = new HubConnection("http://ipwebapp.azurewebsites.net/");
        public static IHubProxy connProxy = _conn.CreateHubProxy("bandHub");
        public static int samples = 0;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Searching...";

            IBandInfo[] bands = await BandClientManager.Instance.GetBandsAsync();
            await _conn.Start();

            if (bands.Length < 1)
            {
                txtStatus.Text = "No Band...";
                return;
            }
            else
            {
                txtStatus.Text = "Band found...";
            }

            try
            {
                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(bands[0]))
                {
                    IEnumerable<TimeSpan> supportedIntervals = bandClient.SensorManager.Accelerometer.SupportedReportingIntervals;
                    bandClient.SensorManager.Accelerometer.ReportingInterval = supportedIntervals.Last();

                    bandClient.SensorManager.Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
                    await bandClient.SensorManager.Accelerometer.StartReadingsAsync();
                    await Task.Delay(TimeSpan.FromSeconds(120));

                    txtStatus.Text = "Band Stopping...";
                    await bandClient.SensorManager.Accelerometer.StopReadingsAsync();
                    txtStatus.Text = "Band Stopped...";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = ex.Message;
            }
        }

        private async void Accelerometer_ReadingChanged(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandAccelerometerReading> e)
        {
            var x = e.SensorReading.AccelerationX;
            var y = e.SensorReading.AccelerationY;
            var z = e.SensorReading.AccelerationZ;

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                samples++;

                txtEventCount.Text = samples.ToString();

                connProxy.Invoke("updateData", y);

                txtCoorX.Text = x.ToString();
                txtCoorY.Text = y.ToString();
                txtCoorZ.Text = z.ToString();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Blink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static string MQTT_BROKER_ADDRESS { set; get; }
        public static MqttClient client { set; get; }
        public static List<bool> devices { set; get; }
        public static Dictionary<int,GpioPin> pins { set; get; }
        public MainPage()
        {
            this.InitializeComponent();
            //setup setting
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (!localSettings.Values.ContainsKey("MQTT_BROKER_ADDRESS"))
            {
                localSettings.Values["MQTT_BROKER_ADDRESS"] = "192.168.100.3";
            }
            MQTT_BROKER_ADDRESS = localSettings.Values["MQTT_BROKER_ADDRESS"].ToString();
            //setup mqtt
            initMqtt();
            //setup devices state
            
            devices = new List<bool>();
            for(int i = 0; i < 27; i++)
            {
                devices.Add(false);
            }
            pins = new Dictionary<int, GpioPin>();
            //setup timer
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            var gpio = GpioController.GetDefault();

            string Status = string.Empty;
            for (int i = 0; i < devices.Count; i++)
            {
                Status += i + ":" + devices[i].ToString() + ";";
            }
            PublishMessage("/raspberry/status", Status);
            
        }
        static void PublishMessage(string Topic, string Pesan)
        {
            client.Publish(Topic, Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
        private void initMqtt()
        {
            client = new MqttClient(MQTT_BROKER_ADDRESS);
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            //subscribe
            client.MqttMsgPublishReceived += async (sender, e) =>
            {

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var gpio = GpioController.GetDefault();
                    // Show an error if there is no GPIO controller
                    if (gpio == null)
                    {
                        GpioStatus.Text = "There is no GPIO controller on this device.";
                        return;
                    }
                    string Message = new string(Encoding.UTF8.GetChars(e.Message));
                    if (Message.IndexOf(":") < 1) return;
                   // handle message received 
                   TxtMsg.Text = "Message Received : " + Message;
                   //switch gpio state
                   string[] pinItem = Message.Split(':');
                    int pinsel = Convert.ToInt32(pinItem[0]);
                    GpioPinValue state = pinItem[1] == "True" ? GpioPinValue.High : GpioPinValue.Low;
                    if (pins.ContainsKey(pinsel)) {
                        pins[pinsel].Write(state);
                    }
                    else {
                        var pin = gpio.OpenPin(pinsel);
                        pin.Write(state);
                        pin.SetDriveMode(GpioPinDriveMode.Output);
                        pins.Add(pinsel, pin);
                    }
                    devices[pinsel] = (state == GpioPinValue.High ? true : false);
                    GpioStatus.Text = string.Format("PIN {0} -> {1}", pinsel, state == GpioPinValue.High ? "ON" : "OFF");

                });
            };
            client.Subscribe(new string[] { "/raspberry/control" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

      
        
    }
}

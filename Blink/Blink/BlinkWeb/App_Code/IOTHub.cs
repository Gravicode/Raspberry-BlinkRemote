using System;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Configuration;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IOT.Web
{
    [Serializable]
    public class IOTDevice
    {
        public int ID { set; get; }
        public bool State { set; get; }
    }

    [HubName("IOTHub")]
    public class IOTHub : Hub
    {
        public static MqttClient client { set; get; }
        public static string MQTT_BROKER_ADDRESS
        {
            get { return ConfigurationManager.AppSettings["MQTT_BROKER_ADDRESS"]; }
        }
        static void SubscribeMessage()
        {
            // register to message received 
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { "/raspberry/status", "/raspberry/control" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }


        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string Pesan = Encoding.UTF8.GetString(e.Message);
            switch (e.Topic)
            {
                case "/raspberry/control":
                    WriteMessage(Pesan);
                    break;
                case "/raspberry/status":
                    WriteMessage(Pesan);
                    UpdateState(Pesan);
                    break;
            }

        }
        public IOTHub()
        {
            if (client == null)
            {
                // create client instance 
                client = new MqttClient(MQTT_BROKER_ADDRESS);
                string clientId = Guid.NewGuid().ToString();
                client.Connect(clientId, "guest", "guest");
                SubscribeMessage();
            }
        }

        [HubMethodName("ToggleSwitch")]
        public void ToggleSwitch(int Pin, bool State)
        {
            string Pesan = Pin + ":" + State.ToString();
            client.Publish("/raspberry/control", Encoding.UTF8.GetBytes(Pesan), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

        }

        internal void WriteRawMessage(string msg)
        {
            WriteMessage(msg);
        }
        internal static void WriteMessage(string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
            dynamic allClients = context.Clients.All.WriteData(message);
        }
        internal static void ChangeState(int PIN, bool Status)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
            dynamic allClients = context.Clients.All.ChangeState(PIN, Status);
        }
        internal static void UpdateState(string message)
        {
            try
            {
                List<IOTDevice> datas = new List<IOTDevice>();
                var context = GlobalHost.ConnectionManager.GetHubContext<IOTHub>();
                foreach (var str in message.Split(';'))
                {
                    if (string.IsNullOrEmpty(str.Trim())) continue;
                    int PIN = int.Parse(str.Split(':')[0]);
                    bool State = bool.Parse(str.Split(':')[1]);
                    IOTDevice node = new IOTDevice() { ID = PIN, State = State };
                    datas.Add(node);
                }

                dynamic allClients = context.Clients.All.UpdateState(JsonConvert.SerializeObject(datas));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
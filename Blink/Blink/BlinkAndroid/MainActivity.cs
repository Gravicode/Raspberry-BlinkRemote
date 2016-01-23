using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.AspNet.SignalR.Client;
using System.Collections.Generic;

namespace BlinkAndroid
{

    [Activity(Label = "BlinkAndroid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        HubConnection hubConnection=null;
        IHubProxy chatHubProxy=null;
        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Connect to the server
            hubConnection = new HubConnection("http://192.168.100.3:4000/");

            // Create a proxy to the 'IOTHub' SignalR Hub
            chatHubProxy = hubConnection.CreateHubProxy("IOTHub");

            // Start the connection
            await hubConnection.Start();


            chatHubProxy.On<List<IOTDevice>>("UpdateState", nodes =>
            {
                var Msg = string.Empty;
                foreach (var item in nodes)
                {
                    Msg += item.ID + " = " + item.State+", ";
                }
                Toast.MakeText(this, Msg, ToastLength.Short).Show();
            });

          

            hubConnection.ConnectionSlow += () =>
            {
               Toast.MakeText(this, "Connection problems.\r\n",ToastLength.Short).Show();
            };
            hubConnection.Error += ex =>
            {
                Toast.MakeText(this, string.Format("SignalR error: {0}\r\n", ex.Message), ToastLength.Short).Show();
            };


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            TableLayout _table = (TableLayout)FindViewById(Resource.Id.table);

            var layoutParams = new TableRow.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);
           
            // create buttons in a loop
            for (int i = 0; i < 27; i++)
            {
                TableRow tableRow = new TableRow(this);
                TextView Lbl = new TextView(this);
                Lbl.Text = "PIN - "+i;
                Button button = new Button(this);
                Button button2 = new Button(this);
                button.Text = "Turn Pin " + i + " ON";
                button2.Text = "Turn Pin " + i + " OFF";
                button.Tag = "ON:"+i;
                button2.Tag = "OFF:" + i;
                // R.id won't be generated for us, so we need to create one
                //button.Id = i;
                button.Click += Button_Click;
                button2.Click += Button_Click;
                button.LayoutParameters = layoutParams;
                button2.LayoutParameters = layoutParams;

                tableRow.AddView(Lbl, 0);
                tableRow.AddView(button, 1);
                tableRow.AddView(button2, 2);

                _table.AddView(tableRow, 0);
            }
          
         
            

        }

        private async void Button_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var selindex = int.Parse(btn.Tag.ToString().Split(':')[1]);
            if (btn.Tag.ToString().Contains("ON"))
            {
                await chatHubProxy.Invoke("ToggleSwitch", selindex, true);
            }
            else
            {
                await chatHubProxy.Invoke("ToggleSwitch", selindex, false);
            }
        }

    }

    [Serializable]
    public class IOTDevice
    {
        public int ID { set; get; }
        public bool State { set; get; }
    }
}


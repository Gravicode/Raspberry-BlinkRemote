using Microsoft.Owin;
using Owin;

namespace IOT.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}

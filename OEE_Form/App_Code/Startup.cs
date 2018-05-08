using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OEE_Form.Startup))]
namespace OEE_Form
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}

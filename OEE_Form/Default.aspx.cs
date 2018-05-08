using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class _Default : Page
{
    public string test;

    protected void Page_Init(object sender, EventArgs e)
    {
        test = "testing";
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}
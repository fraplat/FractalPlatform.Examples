using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Database.Storages;
using System;

namespace FractalPlatform.Examples.Applications.Template
{
    public class TemplateApplication : BaseApplication
    {
        public override void OnStart()
        {
            FirstDocOf("Dashboard")
                  .OpenForm();
        }

        public override bool OnEventDimension(EventInfo eventInfo)
        {
            FirstDocOf(eventInfo.Action)
                  .OpenForm();

            return true;
        }
    }
}

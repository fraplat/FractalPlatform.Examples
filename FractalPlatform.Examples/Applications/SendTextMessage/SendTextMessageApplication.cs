using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;

namespace FractalPlatform.Examples.Applications.SendTextMessage
{
    public class SendTextMessageApplication : BaseApplication
    {
        public override void OnStart() =>
            FirstDocOf("Dashboard")
                  .OpenForm(onSave: result =>
                  {
                          FirstDocOf("Dashboard")
                                .Update("{'TextMessages':[Add,{'Provider':'Telegram','Receiver':@Receiver,'Message':@Message,'IsSent':false}]}",
                                        result.Collection
                                              .Values("{'Receiver':$,'Message':$}")
                                              .ToArray());
                  });
    }
}

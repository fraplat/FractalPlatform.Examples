using FractalPlatform.Database.Engine;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;

namespace FractalPlatform.agency
{
    public class agencyApplication : BaseApplication
    {
        public override void OnStart()
        {
            if(!Context.HasUrlTag ||
                Context.UrlTag == "home")
            {
                FirstDocOf("Home")
                    .OpenForm();
            }
            else if(Context.UrlTag == "audit")
            {
                CreateNewDocFor("NewAudit","Audits")
                    .OpenForm(result => 
                    {
                        DocsWhere("Audits", result.TargetDocID)
                            .Update(@"{'TextMessages':[Add,
                                {'Provider':'Telegram',
                                'Receiver':'5018512422',
                                'Message':'New project audit requested for agency.',
                                'IsSent':false}]}");
                        
                        FirstDocOf("AuditRequested")
                            .OpenForm();
                    });
            }
            else if(Context.UrlTag == "audits")
            {
                DocsOf("Audits")
                    .OpenForm();
            }
            else
            {
                NotImplementedMessageBox();
            }
        }
    }
}
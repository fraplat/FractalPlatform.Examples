using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;

namespace FractalPlatform.Examples.Applications.Vote
{
    public class VoteApplication : BaseApplication
    {
        public override void OnStart() =>
             MergeDocFor("Questionary", "Report")
                  .OpenForm(onSave: result => FirstDocOf("Report").OpenForm());
    }
}

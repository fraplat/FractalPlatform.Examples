using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Clients;

namespace FractalPlatform.Examples.Applications.AIWhatYouSee
{
    public class AIWhatYouSeeApplication : BaseApplication
    {
        public override void OnStart() =>
            FirstDocOf("Dashboard").OpenForm(onSave: result =>
            {
                var question = result.FindFirstValue("Question");
                var image = result.FindFirstValue("Image");

                var bytes = ReadFileBytes(image);

                var answer = AI.Generate(question, AIModel.GPT4oMini, AIAttachment.FromBytes(bytes));

                MessageBox(answer.Text, MessageBoxButtonType.Ok);
            });
    }
}
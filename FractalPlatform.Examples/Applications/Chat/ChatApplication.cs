using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Client.UI.DOM;
using System.Linq;

namespace FractalPlatform.Examples.Applications.Chat
{
    public class ChatApplication : BaseApplication
    {
        public override void OnStart() =>
            FirstDocOf("Chats")
                .OpenForm(onSave: result =>
                          {
                              FirstDocOf("Chats")
                                    .Update("{'Messages':[Add,{'OnDate':@Now,'Who':@Who,'Message':@Message}]}",
                                            result.Collection
                                                  .Values("{'Who':$,'Message':$}")
                                                  .Select(x => x.Replace("<", "&lt;").Replace(">", "&gt;"))
                                                  .ToArray());
                          },
                          onClose: result => OnStart());
        
        public override BaseRenderForm CreateRenderForm(DOMForm form) => new RenderForm(this, form);
    }
}

using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Notepad
{
    public class NotepadApplication : BaseApplication
    {
        public override void OnStart() => FirstDocOf("Notes").OpenForm();
    
        private bool SaveAndClose()
        {
            SaveForm();
            CloseForm();
            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Cancel"  => SaveAndClose(),
                "Save"    => SaveForm(),
                "Refresh" => RefreshForm(),
                _ => base.OnEventDimension(info)
            };
    }
}
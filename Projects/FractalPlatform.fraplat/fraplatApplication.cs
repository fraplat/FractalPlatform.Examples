using FractalPlatform.Database.Engine;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;

namespace FractalPlatform.fraplat {
    public class fraplatApplication: BaseApplication {

        private string GetPage(string tag) =>
            tag switch
            {
                "entry"           => "Entry",
                "basic"           => "Basic",
                "advanced"        => "Advanced",
                "enterprise"      => "Enterprise",
                "pricing"         => "Pricing",
                "about"           => "About",
                "realworld"       => "RealWorld",
                "performance"     => "Performance",
                "hosting"         => "Hosting",
                "storage"         => "Storage",
                "autotesting"     => "AutoTesting",
                "queries"         => "Queries",
                "transactions"    => "Transactions",
                "fractalstudio"   => "FractalStudio",
                "uicontrols"      => "UIControls",
                "baseapplication" => "BaseApplication",
                "formbuilder"     => "FormBuilder",
                "transformations" => "Transformations",
                "layouts"         => "Layouts",
                "guides"          => "Guides",
                "competitors"     => "Competitors",
                _ => null
            };

        public override void OnStart() {
            var page = !Context.HasUrlTag || Context.UrlTag == "home"
                ? "Home"
                : GetPage(Context.UrlTag);

            if (page != null)
                FirstDocOf(page).OpenForm();
            else
                NotImplementedMessageBox();
        }
    }
}
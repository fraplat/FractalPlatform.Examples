using System.Collections.Generic;
using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Client.UI.DOM;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Examples.Applications.OnlineShop
{
    public class OnlineShopApplication : BaseApplication
    {
        private bool OpenProduct(EventInfo info)
        {
            var name = info.Collection
                                .GetWhere(info.AttrPath.Parent)
                                .Value("{'Products':[{'Name':$}]}");

            var categories = info.Collection
                                .GetWhere(info.AttrPath.Parent)
                                .Values("{'Products':[{'Categories':[$]}]}");

            return DocsWhere("Products", "{'Name':@Name,'Categories':[In,@Categories]}", name, categories)
                      .ExtendUIDimension("{'ReadOnly':true}")
                      .OpenForm();
        }

        private bool SearchProducts(EventInfo info)
        {
            var searchText = info.Collection
                                      .Value("{'Header':{'SearchText':$}}");

            var categories = DocsWhere("Products", "{'Name':@SearchText}", searchText)
                                   .ToStorage("{'Categories':[$]}")
                                   .ToAttrList()
                                   .Select(x => x.Value.ToString())
                                   .Distinct()
                                   .Select(x => new { Category = x });

            var products = DocsWhere("Products", "{'Name':@Name}", searchText).ToStorage();

            return info.Collection
                     .SetToArrayPath(categories, "Filters")
                     .SetToArrayPath(products, "Products")
                     .OpenForm();
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Categories"   => DocsOf("Categories").OpenForm(),
                "Products"     => DocsOf("Products").OpenForm(),
                "NewCategory"  => CreateNewDocFor("NewCategory", "Categories").OpenForm(),
                "NewProduct"   => CreateNewDocFor("NewProduct", "Products").OpenForm(),
                "Category"     => OpenCategory(info.AttrValue.ToString()),
                "OpenButton"   => OpenProduct(info),
                "SearchButton" => SearchProducts(info),
                _ => base.OnEventDimension(info)
            };

        public override List<string> OnEnumDimension(EnumInfo info)
        {
            if (info.Variable == "Categories")
            {
                return DocsOf("Categories")
                             .Values("{'Name':$}");
            }
            else
            {
                return base.OnEnumDimension(info);
            }
        }

        public void Filter(FormResult result)
        {
            var category = result.Collection
                                 .Value("{'Header':{'Category':$}}");

            var filters = result.Collection
                                 .GetWhere("{'Filters':[{'Checked':true}]}")
                                 .Values("{'Filters':[{'Query':$}]}");

            Storage products;

            if (filters.Count > 0)
            {
                var orQuery = DocsWhere("Products", filters[0]);

                for (int i = 1; i < filters.Count; i++)
                {
                    orQuery.OrWhere(filters[i]);
                }

                products = DocsWhere("Products", "{'Categories':[Any,@Category]}", category)
                                 .AndWhere(orQuery)
                                 .ToStorage();
            }
            else
            {
                products = DocsWhere("Products", "{'Categories':[Any,@Category]}", category)
                                 .ToStorage();
            }

            result.Collection
                  .SetToArrayPath(products, "Products")
                  .OpenForm(onSave: Filter);
        }

        public bool OpenCategory(string category)
        {
            var collection = FirstDocOf("Dashboard")
                                   .ToCollection();

            collection.Update("{'Header':{'Category':@Category}}", category);

            var filter = DocsWhere("Categories", "{'Name':@Category}", category)
                               .ToStorage("{'Filters':[$]}");

            collection.MergeToPath(filter)
                      .OpenForm(onSave: Filter);

            return true;
        }

        public override void OnStart() => OpenCategory("Cars");

        public override BaseRenderForm CreateRenderForm(DOMForm form) => new RenderForm(this, form);
    }
}
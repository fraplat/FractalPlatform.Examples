using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.CoffeePoints
{
    public class CoffeePointsApplication : BaseApplication
    {
        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Find"    => FirstDocOf("Find").OpenForm(onSave: result => Dashboard(result.Collection.DocumentStorage)),
                "Propose" => CreateNewDocFor("NewPropose", "Proposes")
                                .OpenForm(onSave: result => MessageBox("Thank you, we added your proposition.",
                                                                        "Information",
                                                                        MessageBoxButtonType.Ok,
                                                                        result => Dashboard())),
                _ => base.OnEventDimension(info)
            };

        public override bool OnOpenForm(FormInfo info)
        {
            if (info.AttrPath.FirstPath == "Proposes")
            {
                var dateAndLatAndLng = info.Collection
                                             .GetWhere(info.AttrPath)
                                             .Values("{'Proposes':[{'OnDate':$,'Map':{'Point':{'Lat':$,'Lng':$}}}]}");

                DocsWhere("Proposes", "{'OnDate':@OnDate}", dateAndLatAndLng[0])
                    .ExtendDocument("{'Map':{'Center':{'Lat':@Lat,'Lng':@Lng},'Points':[{'Lat':@Lat,'Lng':@Lng}]}}", dateAndLatAndLng[1], dateAndLatAndLng[2])
                    .OpenForm();

                return false;
            }

            return base.OnOpenForm(info);
        }

        public override object OnComputedDimension(ComputedInfo info)
        {
            if (info.Variable == "Key")
            {
                return "AIzaSyCwOrdckrXQNjKXhiG7ZRsKy0459ck8VDU";
            }
            else if (info.AttrPath.LastPath == "Gender")
            {
                return info.AttrValue.ToString() == "Male" ? "men" : "women";
            }
            else if (info.AttrValue.IsBoolean)
            {
                return info.AttrValue.GetBoolValue() ? "" : "style='display:none'";
            }

            return base.OnComputedDimension(info);
        }

        private void Dashboard(Storage filter = null)
        {
            Storage friends;
            Storage points;

            WantQuery query;

            var minAge = 0;
            var maxAge = 100;

            if (filter != null)
            {
                var where = filter.ToAttrList().Where(x => x.Key.FirstPath != "Gender" &&
                                                           x.Value.GetBoolValue()).ToJson();
                query = DocsWhereFacet("Proposes", where);

                minAge = filter.GetFirstDoc(Context).IntValue("{'Age':{'Min':$}}");
                maxAge = filter.GetFirstDoc(Context).IntValue("{'Age':{'Max':$}}");

                var isMale = filter.GetFirstDoc(Context).BoolValue("{'Gender':{'Male':$}}");
                var isFemale = filter.GetFirstDoc(Context).BoolValue("{'Gender':{'Female':$}}");

                if (isMale && !isFemale)
                {
                    query = query.AndWhere(@"{'Contacts':{'Gender':'Male'}}");
                }
                else if (!isMale && isFemale)
                {
                    query = query.AndWhere(@"{'Contacts':{'Gender':'Female'}}");
                }
            }
            else
            {
                query = DocsOf("Proposes");
            }

            query = query.AndWhere(@"{'OnDate':Range(@StartDate,@EndDate),
                                      'Contacts':{'Age':Range(@MinAge,@MaxAge)}}",
                                        GetNowDate().AddDays(-7), GetNowDate(),
                                        minAge, maxAge);

            friends = query.ToStorage();
            points = query.ToStorage("{'Map':{'Point':!{'Lng':$,'Lat':$}}}");

            FirstDocOf("Dashboard")
            .ToCollection()
            .DeleteByParent("Proposes")
            .MergeToArrayPath(points, new AttrPath("Map", "Points"))
            .IfTrue(filter != null, x => x.MergeToArrayPath(friends, "Proposes"), x => x)
            .OpenForm();
        }

        public override void OnStart() => Dashboard();
    }
}

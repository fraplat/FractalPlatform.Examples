using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Common.Enums;

namespace FractalPlatform.Diary
{
    public class DiaryApplication : BaseApplication
    {
        private int Calculate(uint docID, Collection collection)
        {
            var points = DocsOf("Points")
                            .ToCollection();

            var sumPoints = 0;

            var storage = collection.GetStorage(DimensionType.LifeTime);

            collection
                .ResetDimension(DimensionType.LifeTime)
                .ScanKeysAndValues((attrPath, attrValue) =>
            {
                if (attrValue.GetBoolValue())
                {
                    var currAttrPath = attrPath.Clone();

                    currAttrPath.DocID = Constants.FIRST_DOC_ID;

                    sumPoints += points.GetValueByPath(currAttrPath).GetIntValue();
                }

                return true;
            },
            docID);

            collection.SetDimension(DimensionType.LifeTime, storage);

            return sumPoints;
        }

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "Points" => Calculate(info.DocID, info.Collection),
                _ => base.OnComputedDimension(info)
            };


        private void Dashboard() => FirstDocOf("Dashboard").OpenForm();

        public override void OnStart()
        {
            const string password = "77";

            if (Context.UrlTag == password)
            {
                Dashboard();

                return;
            }

            InputBox("Password", "Enter password", result =>
            {
                if (result.Collection
                          .IsEquals("{'Password':$}", password))
                {
                    Context.UrlTag = password;

                    Dashboard();
                }
                else
                {
                    MessageBox("Wrong password");
                }
            });
        }

        private bool NewDay() =>
            CreateNewDocFor("NewDay", "Days")
                .OpenForm(onSave: result =>
                {
                    var points = Calculate(result.DocID, result.Collection);

                    MessageBox($"Today you have {points} points.", MessageBoxButtonType.Ok);
                });

        private bool Report()
        {
            var number = 0;

            var points = DocsOf("Days")
                        .Values("{'Points':$}")
                        .Select(val => new
                        {
                            X = ++number,
                            Y = double.Parse(val)
                        })
                        .GroupBy(x => x.X / 7,
                                 (k, g) => new
                                 {
                                     X = k,
                                     Y = g.Average(x => x.Y)
                                 })
                        .ToList();
            new
            {
                Control = new
                {
                    Title = new
                    {
                        Name = "My Points",
                        X = "DocID",
                        Y = "Points"
                    },
                    Lines = new[]
                    {
                    new
                    {
                        Name = "Weeks",
                        Points = points
                    }
                }
                }
            }
            .ToCollection("Report")
            .SetUIDimension("{'ReadOnly':true,'Control':{'ControlType':'Chart','Style':'Type:LineGraphs'}}")
            .OpenForm();

            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.AttrPath.ToString() switch
            {
                "Days"   => DocsOf("Days").OpenForm(),
                "NewDay" => NewDay(),
                "Points" => DocsOf("Points").OpenForm(),
                "Report" => Report(),
                _ => base.OnEventDimension(info)
            };
    }
}
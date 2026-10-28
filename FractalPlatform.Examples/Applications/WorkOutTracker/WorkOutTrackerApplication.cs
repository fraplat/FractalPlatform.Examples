using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.App.Chart;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Enums;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Examples.Applications.WorkOutTracker
{
    public class WorkOutTrackerApplication : BaseApplication
    {
        public override bool OnEventDimension(EventInfo info)
        {
            if (info.Action == "Report")
            {
                var exercises = info.Collection
                                         .GetDoc(info.DocID)
                                         .Values("{'Results':[{'Exercise':$}]}")
                                         .Distinct();

                //create chart
                var chart = new LineGraphsChartInfo
                {
                    Title = new TitleChartInfo
                    {
                        Name = "Exercises",
                        X = "Time",
                        Y = "Value"
                    },
                    Lines = exercises.Select(e =>
                    {
                        uint x = 0;

                        var line = new LineChartInfo
                        {
                            Name = e,
                            Points = info.Collection
                                              .GetDoc(info.DocID)
                                              .AndWhere("{'Results':[{'Exercise':@Exercise}]}", e)
                                              .UIntValues("{'Results':[{'Sum':$}]}")
                                              .Select(y => new PointChartInfo { X = x++, Y = y })
                                              .ToList()
                        };

                        return line;
                    }).ToList()
                };

                //open report
                DQL("{'Chart':@Chart}", chart)
                    .ToCollection()
                    .SetUIDimension("{'ReadOnly':true,'Chart':{'ControlType':'Chart'}}")
                    .OpenForm();
            }
            else //if (info.Action == "Configure")
            {
                Client.SetDefaultCollection("WorkOut")
                      .SetDefaultDimension(DimensionType.Enum)
                      .WantModifyExistingDocument()
                      .OpenForm();
            }

            return false;
        }

        public override void OnStart() => DocsOf("WorkOut").OpenForm();
    }
}

using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using System;
using System.Globalization;
using System.Linq;

namespace FractalPlatform.BowlingScores
{
    public class BowlingScoresApplication : BaseApplication
    {

        public override void OnStart() => FirstDocOf("Dashboard").OpenForm();

        private bool NewScore() =>
            FirstDocOf("NewScore").OpenForm(onSave: result =>
            {
                var data = new
                {
                    OnDate = DateTime.Now,
                    Image = result.FindFirstValue("Image"),
                    Viacheslav = new
                    {
                        Points = result.IntValue("{'Scores':{'Viacheslav':{'Points':$}}}"),
                        Strikes = result.IntValue("{'Scores':{'Viacheslav':{'Strikes':$}}}")
                    },
                    Julia = new
                    {
                        Points = result.IntValue("{'Scores':{'Julia':{'Points':$}}}"),
                        Strikes = result.IntValue("{'Scores':{'Julia':{'Strikes':$}}}")
                    }
                };

                AddDoc("Scores", data);
            });

        public override bool OnEventDimension(EventInfo info) =>
            info.AttrPath.ToString() switch
            {
                @"Score\NewScore"  => NewScore(),
                @"Score\Scores"    => DocsOf("Scores").OpenForm(),
                @"Report\Points"   => Report("Points"),
                @"Report\Strikes"  => Report("Strikes"),
                _ => base.OnEventDimension(info)
            };

        private bool Report(string type)
        {
            var janeScores = DocsOf("Scores")
                                .SelectValues("{'OnDate':$,'Jane':{@Type:$}}", type)
                                .Select(vals => new
                                {
                                    Name = "Jane",
                                    Value = double.Parse(vals[1]),
                                    Group = DateTime.Parse(vals[0], CultureInfo.InvariantCulture).ToString("yyyyMMdd")
                                })
                                .ToList();

            var bobScores = DocsOf("Scores")
                                    .SelectValues("{'OnDate':$,'Bob':{@Type:$}}", type)
                                    .Select(vals => new
                                    {
                                        Name = "Bob",
                                        Value = double.Parse(vals[1]),
                                        Group = DateTime.Parse(vals[0], CultureInfo.InvariantCulture).ToString("yyyyMMdd")
                                    })
                                    .ToList();

            var chartData = new
            {
                Control = new
                {
                    Title = new { Name = type, X = "OnDate", Y = type },
                    Columns = janeScores.Union(bobScores).ToList()
                }
            };

            chartData.ToCollection("Bowling " + type)
                     .SetUIDimension("{'ReadOnly':true,'Control':{'ControlType':'Chart','Style':'Type:Bar'}}")
                     .OpenForm();

            return true;
        }
    }
}
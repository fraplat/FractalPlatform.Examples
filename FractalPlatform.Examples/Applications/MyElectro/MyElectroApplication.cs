using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Enums;
using FractalPlatform.Database.Engine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FractalPlatform.Examples.Applications.MyElectro
{
    public class MyElectroApplication : BaseApplication
    {
        private const string ScheduleUrl = "https://www.dtek-kem.com.ua/ua/shutdowns";

        private const string FactMarker = "DisconSchedule.fact";

        private const string DefaultGroupId = "GPV25.1";

        private const long SecondsPerDay = 24 * 60 * 60;

        private const string Present = "ПРИСУТНЄ";

        private const string Absent = "ВІДСУТНЄ";

        private const string Unknown = "НЕВІДОМО";

        private static string _lastKnownFact;

        private static DateTime _lastKnownFactDate;

        // Values of DisconSchedule.preset.time_type: a half-hour resolution state of each hour.
        private static (string FirstHalf, string SecondHalf) GetElectricity(string timeType)
        {
            switch (timeType)
            {
                case "yes":
                    return (Present, Present);
                case "no":
                    return (Absent, Absent);
                case "first":
                    return (Absent, Present);
                case "second":
                    return (Present, Absent);
                case "mfirst":
                    return (Unknown, Present);
                case "msecond":
                    return (Present, Unknown);
                default:
                    return (Unknown, Unknown);
            }
        }

        private object GetSchedule(string schedule, bool isTomorrow)
        {
            string prevElectricity = null;
            int groupId = 0;
            return schedule.ToCollection()
                   .ToAttrList()
                   .Select(x =>
                   {
                       var hour = int.Parse(x.Key.FirstPath);
                       var startHour = hour - 1;
                       var endHour = hour;
                       var (firstHalf, secondHalf) = GetElectricity(x.Value.ToString());
                       var result = new List<dynamic>();

                       void Append(int fromHour, int fromMinute, int toHour, int toMinute, string electricity)
                       {
                           if (prevElectricity == null || prevElectricity != electricity)
                           {
                               groupId++;
                               prevElectricity = electricity;
                           }

                           result.Add(new
                           {
                               StartHour = fromHour,
                               StartMinute = fromMinute,
                               EndHour = toHour,
                               EndMinute = toMinute,
                               Electricity = electricity,
                               GroupId = groupId
                           });
                       }

                       if (firstHalf == secondHalf)
                       {
                           Append(startHour, 0, endHour, 0, firstHalf);
                       }
                       else
                       {
                           Append(startHour, 0, startHour, 30, firstHalf);
                           Append(startHour, 30, endHour, 0, secondHalf);
                       }

                       return result;
                   })
                  .SelectMany(x => x)
                  .GroupBy(x => x.GroupId,
                  (k, g) => new
                  {
                      StartHour = g.Min(x => x.StartHour),
                      StartMinute = g.First().StartMinute,
                      EndHour = g.Max(x => x.EndHour),
                      EndMinute = g.Last().EndMinute,
                      Electricity = g.First().Electricity
                  })
                  .Where(x => (DateTime.Now.Hour < x.EndHour ||
                               (DateTime.Now.Hour == x.EndHour &&
                                DateTime.Now.Minute < x.EndMinute) ||
                                isTomorrow))
                  .Select(x =>
                  {
                      var start = new TimeSpan(x.StartHour, x.StartMinute, 0);
                      var end = new TimeSpan(x.EndHour, x.EndMinute, 0);
                      var total = end - start;
                      var sign = x.Electricity == Present ? "+" : "-";
                      var isUndefined = x.StartHour == 0 &&
                                        x.StartMinute == 0 &&
                                        x.EndHour == 24 &&
                                        x.EndMinute == 0;

                      return new
                      {
                          Час = $"{x.StartHour:00}:{x.StartMinute:00} - {x.EndHour:00}:{x.EndMinute:00}",
                          Світло = !isUndefined || !isTomorrow ? x.Electricity : Unknown,
                          Годин = !isUndefined ? $"{sign} {(int)total.TotalHours:00}:{total.Minutes:00}" : null
                      };
                  }).ToList();
        }

        private static string UnknownSchedule() =>
            "{" + string.Join(",", Enumerable.Range(1, 24).Select(hour => $"\"{hour}\":\"maybe\"")) + "}";

        private string DownloadPage()
        {
            // The site is behind a bot filter which answers with a challenge page to header-less requests.
            var headers = new Dictionary<string, string>
            {
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
                ["Accept-Language"] = "uk-UA,uk;q=0.9,en-US;q=0.8,en;q=0.7",
                ["Upgrade-Insecure-Requests"] = "1"
            };

            foreach (var header in headers)
            {
                REST.AddHeader(header.Key, header.Value);
            }

            try
            {
                return REST.Get(ScheduleUrl);
            }
            finally
            {
                foreach (var header in headers)
                {
                    REST.RemoveHeader(header.Key);
                }
            }
        }

        private static string ExtractFactJson(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            var markerIndex = html.IndexOf(FactMarker, StringComparison.Ordinal);

            if (markerIndex < 0)
            {
                return null;
            }

            var startIndex = html.IndexOf('{', markerIndex);

            if (startIndex < 0)
            {
                return null;
            }

            // The assignment is no longer the last statement of its <script> block,
            // so the object has to be cut out by matching braces instead of by "</script>".
            var depth = 0;
            var inString = false;
            var isEscaped = false;

            for (var i = startIndex; i < html.Length; i++)
            {
                var symbol = html[i];

                if (inString)
                {
                    if (isEscaped)
                    {
                        isEscaped = false;
                    }
                    else if (symbol == '\\')
                    {
                        isEscaped = true;
                    }
                    else if (symbol == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (symbol == '"')
                {
                    inString = true;
                }
                else if (symbol == '{')
                {
                    depth++;
                }
                else if (symbol == '}' && --depth == 0)
                {
                    return html.Substring(startIndex, i - startIndex + 1);
                }
            }

            return null;
        }

        private static JToken FindDay(JObject fact, bool isTomorrow)
        {
            if (!(fact["data"] is JObject days) || !days.HasValues)
            {
                return null;
            }

            // The payload now reports the current day explicitly and may contain a single day
            // only (tomorrow is published later), so days must be addressed by key, not by index.
            if (long.TryParse(fact["today"]?.ToString(), out var today))
            {
                return days[(isTomorrow ? today + SecondsPerDay : today).ToString()];
            }

            var ordered = days.Properties()
                              .OrderBy(x => long.TryParse(x.Name, out var timestamp) ? timestamp : long.MaxValue)
                              .ToList();

            var index = isTomorrow ? 1 : 0;

            return index < ordered.Count ? ordered[index].Value : null;
        }

        private string DownloadFactJson()
        {
            var json = ExtractFactJson(DownloadPage());

            if (json != null)
            {
                _lastKnownFact = json;
                _lastKnownFactDate = DateTime.Now.Date;

                return json;
            }

            // The bot filter blocks bursts of requests, so a schedule already fetched today
            // is a far better answer than wiping the form. It is never reused across days.
            if (_lastKnownFact != null && _lastKnownFactDate == DateTime.Now.Date)
            {
                return _lastKnownFact;
            }

            throw new InvalidOperationException("сторінка не містить розкладу (ймовірно, запит заблоковано захистом від ботів)");
        }

        private string GetDayHours(JObject fact, bool isTomorrow)
        {
            var dtekGroupId = Context.HasUrlTag ? Context.UrlTag : DefaultGroupId;
            var hours = FindDay(fact, isTomorrow)?[dtekGroupId];

            return hours != null ? hours.ToString() : UnknownSchedule();
        }

        public override void OnStart()
        {
            //1. Download data
            var period = TimeSpan.FromMinutes(15);
            var label = "ДТЕК прогнозує наявність світла:";
            var today = UnknownSchedule();
            var tomorrow = UnknownSchedule();

            try
            {
                // A single page download feeds both days: the site throttles repeated requests.
                var fact = JObject.Parse(UseCache(() => DownloadFactJson(), period, "Fact"));

                today = GetDayHours(fact, false);
                tomorrow = GetDayHours(fact, true);
            }
            catch (Exception exception)
            {
                label = $"Не вдалося отримати дані ДТЕК: {exception.Message}";
            }

            //2. Show data
            new
            {
                Label = label,
                Сьогодні = GetSchedule(today, false),
                Завтра = GetSchedule(tomorrow, true)
            }
            .ToCollection("Моніторинг")
            .SetUIDimension("{'Style':'Hide:Number;Cancel:Refresh','ReadOnly':true,'Label':{'ControlType':'Label'}}")
            .SetDimension(DimensionType.Theme, "{'DefaultTheme': 'LightBlue', 'ChooseThemeOnLoginPage': false, 'ChooseThemeOnAllPages': false}")
            .OpenForm();
        }
    }
}
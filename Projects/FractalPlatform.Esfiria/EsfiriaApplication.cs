using System.Collections.Generic;
using System;
using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Enums;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Esfiria
{
    public class EsfiriaApplication : BaseApplication
    {
        private class Period
        {
            public DateTime FromDate { get; set; }

            public DateTime ToDate { get; set; }
        }

        private class Tour
        {
            public string Code { get; set; }

            public string Title { get; set; }

            public string Type { get; set; }

            public string About { get; set; }

            public string Duration { get; set; }

            public string FromCity { get; set; }

            public List<Period> Periods { get; set; }
        }

        private bool BookTour(EventInfo info)
        {
            var tours = DocsOf("Tours").Values("{'Title':$}");

            new
            {
                Title = tours.First(),
            }
            .ToCollection("Choose a tour")
            .SetDimension(DimensionType.Enum, "{'Title':{'Items':[@Items]}}", tours)
            .SetUIDimension("{'Style':'Save:Choose'}")
            .OpenForm(onSave: result =>
            {
                var title = result.FindFirstValue("Title");

                var tour = DocsWhere("Tours", "{'Title':@Title}", title)
                                .SelectOne<Tour>();

                var periods = tour.Periods.Select(x => $"[{x.FromDate}] - [{x.ToDate}]");

                new
                {
                    Title = title,
                    Period = periods.First(),
                }
                .ToCollection("Choose a period")
                .SetDimension(DimensionType.Enum, "{'Period':{'Items':[@Items]}}", periods)
                .SetUIDimension("{'Style':'Save:Book'}")
                .OpenForm(onSave: result =>
                {
                    var period = result.FindFirstValue("Period");

                    var name = info.FindFirstValue("Name");
                    var phone = info.FindFirstValue("Phone");
                    var email = info.FindFirstValue("Email");
                    var description = info.FindFirstValue("Description");

                    var book = new
                    {
                        OnDate = DateTime.Now,
                        Name = name,
                        Phone = phone,
                        Email = email,
                        Description = description,
                        Tour = new
                        {
                            Code = tour.Code,
                            Title = title,
                            Period = period,
                        }
                    };

                    AddDoc("Booked", book);

                    DocsWhere("Requests", info.AttrPath)
                        .Update("{'IsBooked':true}");
                });
            });

            return true;
        }

        private bool ScheduleByMonth()
        {
            var tours = DocsOf("Tours").Select<Tour>();

            new
            {
                Months =
                    Enumerable.Range(1, 12)
                    .Select(month => new
                    {
                        Month = new DateTime(2025, month, 1).ToString("MMMM"),
                        Tours = tours.Where(x => x.Periods.Any(y => y.FromDate > DateTime.Now &&
                                                                    y.FromDate.Month == month))
                                     .ToList()
                    })
            }
            .ToCollection("Months")
            .OpenForm();

            return true;
        }

        private bool SendRequest() =>
            CreateNewDocFor("NewRequest", "Requests")
                .OpenForm(onSave: result =>
                {
                    var receiver = DocsWhere("Users", "{'Name':'Admin'}")
                                    .Value("{'Telegram':$}");

                    var description = result.FindFirstValue("Description");

                    DocsWhere("Users", "{'Name':'Admin'}")
                    .Update(@"{'TextMessages':[Add,
                                    {'Provider':'Telegram',
                                     'Receiver':@Receiver,
                                     'Message':@Message,
                                     'IsSent':false}]}",
                            receiver,
                            $"You have new request of trip: {description}.");

                    MessageBox("Спасибо, Ваш запрос принят в обработку");
                });

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Tours"           => DocsOf("Tours").OpenForm(),
                "About"           => NotImplementedMessageBox(),
                "Contacts"        => NotImplementedMessageBox(),
                "Feedback"        => NotImplementedMessageBox(),
                "Requests"        => DocsOf("Requests").OpenForm(),
                "Booked"          => DocsOf("Booked").OpenForm(),
                "BookTour"        => BookTour(info),
                "ScheduleByMonth" => ScheduleByMonth(),
                "OneDayTours"     => DocsWhere("Tours", "{'Duration':'OneDay'}").OpenForm(),
                "ToursWithNights" => DocsWhere("Tours", "{'Duration':'WithNights'}").OpenForm(),
                "AllTours"        => DocsOf("Tours").OpenForm(),
                "UserAdmin"       => DocsOf("Users").OpenForm(),
                "Send"            => SendRequest(),
                _ => base.OnEventDimension(info)
            };

        private void Dashboard()
        {
            FirstDocOf("Dashboard").OpenForm();
        }

        private void AdminDashboard()
        {
            FirstDocOf("AdminDashboard").OpenForm();
        }

        public override void OnStart()
        {
            if (Context.UrlTag == "admin")
            {
                InputBox("Password", "Enter password", onSave: result =>
                {
                    var currPassword = result.FindFirstValue("Password");

                    if (currPassword == "123")
                    {
                        AdminDashboard();
                    }
                    else
                    {
                        MessageBox("Wrong credentials.");
                    }
                });
            }
            else
            {
                Dashboard();
            }
        }
    }
}

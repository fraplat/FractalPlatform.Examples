using System;
using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Examples.Applications.FreelanceResponse
{
    public class FreelanceResponseApplication : DashboardApplication
    {
        public TaskInfo GetTask(uint docID) => DocsWhere("Tasks", docID).SelectOne<TaskInfo>();

        private string GetDemoWho(AttrPath attrPath) => DocsWhere("Tasks", attrPath).Value("{'Demos':[{'Who':$}]}");

        private uint GetCustomerUserID(uint docID)
        {
            var task = GetTask(docID);

            return DocsWhere("Users", "{'Name':@Who}", task.Who).GetFirstID();
        }

        private uint GetDemoUserID(AttrPath attrPath)
        {
            var who = GetDemoWho(attrPath);

            return DocsWhere("Users", "{'Name':@Who}", who)
                         .GetFirstID();
        }

        private void RejectTask(uint docID)
        {
            var userID = GetCustomerUserID(docID);

            DocsWhere("Users", userID)
                  .Update("{'Rates':[Add,{'Who':'Bot','OnDate':@Now,'Comment':'Task rejected','Stars':1}]}");

            DocsWhere("Tasks", docID)
                  .Update("{'Status':'Rejected'}");
        }

        public override bool OnTimerDimension(TimerInfo info)
        {
            if (info.Action == "Expired")
            {
                var expired = DateTime.Now.AddDays(-3);

                var docIDs = DocsWhere("Tasks", "{'Status':Any('New','InProgress'),'EndDate':Less(@Expired)}",
                                                expired)
                                   .GetDocIDs();

                foreach (var docID in docIDs)
                {
                    RejectTask(docID);
                }
            }

            return true;
        }

        private object ComputeNewAccept(ComputedInfo info)
        {
            var task = GetTask(info.GrandDocID);

            return task.Who != Context.User.Name &&
                       !task.Accepts.Any(x => x.Who == Context.User.Name) &&
                       (string.IsNullOrEmpty(task.Developer) || task.TenderModel == TenerModelType.TheBestDemo) &&
                       (task.Status == StatusType.New || task.Status == StatusType.InProgress);
        }

        private object ComputeNewDemo(ComputedInfo info)
        {
            var task = GetTask(info.GrandDocID);

            return task.Accepts.Any(x => x.Who == Context.User.Name) &&
                   !task.Demos.Any(x => x.Who == Context.User.Name) &&
                   (task.TenderModel == TenerModelType.TheBestDemo ||
                    (task.TenderModel == TenerModelType.TheBestDeveloper && task.Developer == Context.User.Name)) &&
                   (task.Status == StatusType.New || task.Status == StatusType.InProgress);
        }

        private object ComputeRating(ComputedInfo info)
        {
            var count = DocsWhere("Users", info.AttrPath)
                              .Count("{'Rates':[{'Stars':$}]}");

            var sum = DocsWhere("Users", info.AttrPath)
                            .Sum("{'Rates':[{'Stars':$}]}");

            return count > 0 ? sum / count : 0;
        }

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "NewTaskNumber" => "T-" + (DocsOf("Tasks").Count() + 1).ToString("00000"),
                "Rating"        => ComputeRating(info),
                "NewAccept"     => ComputeNewAccept(info),
                "NewDemo"       => ComputeNewDemo(info),
                _ => base.OnComputedDimension(info)
            };

        private bool OpenTasksOrMessage(string query, string emptyMessage)
            {
                var q = DocsWhere("Tasks", query);
                if (q.Any()) q.OpenForm();
                else MessageBox(emptyMessage, MessageBoxButtonType.Ok);
                return true;
            }

            private bool ViewTasks() =>
                OpenTasksOrMessage("{'Status':'New'}", "We don't have any new available tasks. Please, check later.");

            private bool ViewMyTasks()
            {
                var query = DocsWhere("Tasks", "{'Who':@UserName}")
                                  .OrWhere("{'Accepts':[{'Who':@UserName}]}");
                if (query.Any()) query.OpenForm();
                else MessageBox("You don't have any acceptances for tasks.", MessageBoxButtonType.Ok);
                return true;
            }

            private bool ViewUserTasks(EventInfo info)
            {
                var user = DocsWhere("Users", info.AttrPath).Value("{'Name':$}");
                var query = DocsWhere("Tasks", "{'Who':@UserName}")
                                  .OrWhere("{'Accepts':[{'Who':@User}]}", user);
                if (query.Any()) query.OpenForm();
                else MessageBox("User has no any completed tasks yet.", MessageBoxButtonType.Ok);
                return true;
            }

            private bool ViewCompletedTasks() =>
                OpenTasksOrMessage("{'Status':'Completed'}", "We don't have completed tasks yet.");

            private bool NewCustomerRate(EventInfo info)
            {
                var userID = GetCustomerUserID(info.DocID);
                var task = GetTask(info.DocID);

                if (DocsWhere("Users", userID)
                          .AndWhere("{'Rates':[{'TaskNumber':@TaskNumber,'Who':@UserName}]}", task.TaskNumber)
                          .Any())
                {
                    MessageBox("Customer already rated for this task", MessageBoxButtonType.Ok);
                }
                else
                {
                    CreateNewDocForArray("NewRate", "Users", "{'Rates':[$]}", userID)
                          .ExtendDocument("{'TaskNumber':@TaskNumber}", task.TaskNumber)
                          .OpenForm(onSave: result => MessageBox("Thank you. Customer was rated.", MessageBoxButtonType.Ok));
                }

                return true;
            }

            private bool NewUserRate(EventInfo info)
            {
                var userID = GetDemoUserID(info.AttrPath);
                var task = GetTask(info.DocID);

                if (DocsWhere("Users", userID)
                         .AndWhere("{'Rates':[{'TaskNumber':@TaskNumber}]}", task.TaskNumber)
                         .Any())
                {
                    MessageBox("Developer already rated for this task", MessageBoxButtonType.Ok);
                }
                else
                {
                    CreateNewDocForArray("NewRate", "Users", "{'Rates':[$]}", userID)
                          .ExtendDocument("{'TaskNumber':@TaskNumber}", task.TaskNumber)
                          .OpenForm(onSave: result => MessageBox("Thank you. Developer was rated.", MessageBoxButtonType.Ok));
                }

                return true;
            }

            private bool NewAccept(EventInfo info)
            {
                var task = GetTask(info.DocID);

                CreateNewDocForArray("NewAccept", "Tasks", "{'Accepts':[$]}", info.DocID)
                      .ExtendDocument("{'MinPrice':@MinPrice}", task.MinPrice)
                      .OpenForm(onSave: result =>
                      {
                          MessageBox("Thank you. Customer will review your accept.", MessageBoxButtonType.Ok);
                          info.Collection.ReloadData();
                      });

                return true;
            }

            private bool NewDemo(EventInfo info)
            {
                CreateNewDocForArray("NewDemo", "Tasks", "{'Demos':[$]}", info.DocID)
                      .OpenForm(onSave: result =>
                      {
                          MessageBox("Thank you. Your demo is added.", MessageBoxButtonType.Ok);
                          info.Collection.ReloadData();
                      });

                return true;
            }

            private bool RejectTaskAction(EventInfo info)
            {
                MessageBox("Are you sure that you want to reject the Task ? It would decrease your rating.",
                           "Reject task",
                           MessageBoxButtonType.YesNo,
                           onSave: result =>
                           {
                               RejectTask(info.DocID);
                               MessageBox("Task was rejected.", MessageBoxButtonType.Ok);
                               info.Collection.ReloadData();
                           });
                return true;
            }

            private bool AcceptDeveloper(EventInfo info)
            {
                var developer = DocsWhere("Tasks", info.AttrPath)
                                      .Value("{'Accepts':[{'Who':$}]}");

                DocsWhere("Tasks", info.AttrPath)
                      .Update("{'Developer':@Developer,'Status':'InProgress'}", developer);

                DocsWhere("Users", "{'Name':@Developer}", developer)
                      .Update("{'Accepted':Add(1)}");

                MessageBox("Developer was accepted.", MessageBoxButtonType.Ok);
                info.Collection.ReloadData();

                return true;
            }

            private bool AcceptDemo(EventInfo info)
            {
                var developer = DocsWhere("Tasks", info.AttrPath)
                                      .Value("{'Demos':[{'Who':$}]}");

                DocsWhere("Tasks", info.AttrPath)
                      .Update("{'Developer':@Developer,'Status':'Completed'}", developer);

                DocsWhere("Users", "{'Name':@Developer}", developer)
                      .Update("{'Completed':Add(1)}");

                MessageBox("Demo was accepted.", MessageBoxButtonType.Ok);
                info.Collection.ReloadData();

                return true;
            }

            private bool SendMessage(EventInfo info)
            {
                var message = info.Collection
                                       .GetWhere(info.AttrPath)
                                       .Value("{'Demos':[{'Message':$}]}");

                var avatar = DocsWhere("Users", "{'Name':@UserName}")
                                       .Value("{'Photo':$}");

                if (!string.IsNullOrEmpty(message))
                {
                    DocsWhere("Tasks", info.AttrPath)
                          .Update("{'Demos':[{'Chat':[Add,{'Avatar':@Avatar,'OnDate':@Now,'Who':@UserName,'Text':@Message}]}]}",
                                  avatar,
                                  message);

                    info.Collection.ReloadData();
                }
                else
                {
                    MessageBox("You try to send empty message");
                }

                return true;
            }

            public override bool OnEventDimension(EventInfo info) =>
                info.Action switch
                {
                    "Users"           => DocsOf("Users").OpenForm(),
                    "Tasks"           => ViewTasks(),
                    "MyTasks"         => ViewMyTasks(),
                    "UserTasks"       => ViewUserTasks(info),
                    "CompletedTasks"  => ViewCompletedTasks(),
                    "NewTask"         => CreateNewDocFor("NewTask", "Tasks").OpenForm(onSave: result => MessageBox("Thank you. Your task is created.", MessageBoxButtonType.Ok)),
                    "NewCustomerRate" => NewCustomerRate(info),
                    "NewUserRate"     => NewUserRate(info),
                    "NewAccept"       => NewAccept(info),
                    "NewDemo"         => NewDemo(info),
                    "Reject"          => RejectTaskAction(info),
                    "AcceptDeveloper" => AcceptDeveloper(info),
                    "AcceptDemo"      => AcceptDemo(info),
                    "SendMessage"     => SendMessage(info),
                    "Reports"         => NotImplementedMessageBox(),
                    _ => base.OnEventDimension(info)
                };

        public override void OnLogin(FormResult result) => FirstDocOf("Dashboard").OpenForm();
    }
}
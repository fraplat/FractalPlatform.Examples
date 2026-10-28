using System;
using System.Linq;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Enums;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Database.Dimensions.Filter;
using System.Collections.Generic;

namespace FractalPlatform.UTube
{
    public class UTubeApplication : DashboardApplication
    {
        private void Dashboard(string filter = "")
        {
            const int topVideos = 8;

            var collection = DocsWhere("Channels", "{'IsLocked':false}").ToCollection();

            var dimension = (FilterDimension)collection.GetDimension(DimensionType.Filter);
            dimension.SetFilter(Context, filter);

            var allChannels = collection.GetAll().Select<ChannelInfo>();

            IEnumerable<VideoInfo> newVideos;
            IEnumerable<VideoInfo> subscribes;
            IEnumerable<VideoInfo> recommendations;

            if (!Context.User.IsGuest)
            {
                var subNames = DocsWhere("Users", "{'Name':@UserName}").Values("{'Subscribes':[$]}");

                var history = DocsWhere("Users", "{'Name':@UserName}").Values("{'History':[{'UID':$}]}");

                newVideos = allChannels.Where(x => !subNames.Contains(x.Name)) //not in my subscribes
                                       .SelectMany(x => x.Videos)
                                       .Where(x => !history.Contains(x.UID))  //not in my history
                                       .OrderByDescending(x => x.OnDate)      //only fresh videos
                                       .Take(topVideos);                      //first 5 fresh videos

                subscribes = allChannels.Where(x => subNames.Contains(x.Name)) //only my subscribes
                                        .SelectMany(x => x.Videos)
                                        .Where(x => !history.Contains(x.UID)) //not in my history
                                        .OrderByDescending(x => x.CountViews);//rate by views

                recommendations = allChannels.Where(x => !subNames.Contains(x.Name)) //not in my subscribes
                                             .SelectMany(x => x.Videos)
                                             .Where(x => !history.Contains(x.UID)) //not in my history
                                             .OrderByDescending(x => x.CountViews) //rate by views
                                             .Take(topVideos); //only first 5 videos
            }
            else
            {
                newVideos = allChannels.SelectMany(x => x.Videos)
                                       .OrderByDescending(x => x.OnDate) //only fresh videos
                                       .Take(topVideos);                  //first 10 fresh videos

                subscribes = allChannels.SelectMany(x => x.Videos)
                                             .OrderByDescending(x => x.CountViews) //rate by views
                                             .Skip(topVideos)
                                             .Take(topVideos);                      //only first 5 videos

                recommendations = allChannels.SelectMany(x => x.Videos)
                                             .OrderByDescending(x => x.CountViews) //rate by views
                                             .Take(topVideos);                      //only first 5 videos
            }

            FirstDocOf("Dashboard")
                      .ToCollection()
                      .ExtendDocument("{'FilterText':@Filter}", filter)
                      .SetToArrayPath(newVideos, "NewVideos")
                      .SetToArrayPath(subscribes, "Subscribes")
                      .SetToArrayPath(recommendations, "Recommendations")
                      .OpenForm();
        }

        public override void OnLogin(FormResult result) => Dashboard();

        public override void OnRegister(FormResult result) => Dashboard();

        private void OpenVideo(string uid)
        {
            Context.UrlTag = uid;

            if (!Context.User.IsGuest)
            {
                if (!DocsWhere("Users", "{'Name':@UserName,'History':[Any,{'UID':@UID}]}", uid)
                     .Exists())
                {
                    var storage = DocsWhere("Channels", "{'Videos':[{'UID':@UID}]}", uid)
                                  .ToStorage("{'Videos':[!{'Name':$,'Description':$,'UID':$,'OnDate':$}]}");

                    DocsWhere("Users", "{'Name':@UserName}")
                    .Update("{'History':[Add,@Video]}", storage.ToJson(Context, storage.GetFirstDocID(Context)));
                }
            }

            DocsWhere("Channels", "{'Videos':[{'UID':@UID}]}", uid)
            .Update("{'Videos':[{'CountViews':Add(1)}]}");

            var query = DocsWhere("Channels", "{'Videos':[{'UID':@UID}]}", uid);
            var channel = query.ToStorage("{'Name':$,'Photo':$}");
            var video = query.ToStorage("{'Videos':[!$]}");

            FirstDocOf("VideoDashboard")
                .ToCollection()
                .MergeToPath(video)
                .MergeToPath(channel, "Channel")
                .OpenForm(result => Dashboard());
        }

        public override bool OnOpenForm(FormInfo info)
        {
            if (info.Collection.Name == "Channels" &&
                info.AttrPath.FirstPath == "Videos")
            {
                var uid = DocsWhere("Channels", info.AttrPath)
                          .Value("{'Videos':[{'UID':$}]}");

                OpenVideo(uid);

                return false;
            }
            else if (info.Collection.Name == "Users" &&
                     info.AttrPath.FirstPath == "History")
            {
                var uid = info.Collection
                                  .GetWhere(info.AttrPath)
                                  .Value("{'History':[{'UID':$}]}");

                OpenVideo(uid);

                return false;
            }

            return true;
        }

        public bool HasSubscription(uint docID)
        {
            var channel = DocsWhere("Channels", docID)
                          .Value("{'Name':$}");

            return DocsWhere("Users", "{'Name':@UserName,'Subscribes':[Any,@Channel]}", channel)
                   .Exists();
        }

        private object OnDateLabel(ComputedInfo info)
        {
            var dateAgo = DateTime.Now.Subtract(info.Collection
                                                                .GetWhere(info.AttrPath)
                                                                .DateTimeValue("{'Videos':[{'OnDate':$}]}"));

            if (dateAgo.Days / 365 > 0) return $"{dateAgo.Days / 365} years ago";
            if (dateAgo.Days / 30 > 0) return $"{dateAgo.Days / 30} months ago";
            if (dateAgo.Days > 0) return $"{dateAgo.Days} days ago";
            if (dateAgo.Hours > 0) return $"{dateAgo.Hours} hours ago";
            if (dateAgo.Minutes > 0) return $"{dateAgo.Minutes} minutes ago";
            else return $"{dateAgo.Seconds} seconds ago";
        }

        public override object OnComputedDimension(ComputedInfo info)
        {
            var owner = DocsWhere("Channels", info.GrandDocID)
                        .Value("{'Owner':$}");

            return info.Variable switch
            {
                "Subscribe"     => !HasSubscription(info.GrandDocID) && Context.User.Name != owner && !Context.User.IsGuest,
                "Unsubscribe"   => HasSubscription(info.GrandDocID) && Context.User.Name != owner && !Context.User.IsGuest,
                "NewVideo"      => owner == Context.User.Name,
                "Avatar"        => Context.User.GetDefaultAvatar("Guest.png"),
                "Preview"       => info.Collection.GetWhere(info.AttrPath).Value("{'Videos':[{'Video':$}]}").Replace(".mp4", ".jpg"),
                "MyUserLabel"   => Context.User.Name.ToUpper().Substring(0, 2),
                "CountLikes"    => DocsWhere("Channels", info.AttrPath).Count("{'Videos':[{'Likes':[$]}]}") - 1,
                "CountComments" => DocsWhere("Channels", info.AttrPath).Count("{'Videos':[{'Comments':[{'Who':$}]}]}"),
                "OnDateLabel"   => OnDateLabel(info),
                _ => base.OnComputedDimension(info)
            };
        }

        public override bool OnSecurityDimension(SecurityInfo info)
        {
            if (info.Variable == "MyUser" &&
               (info.OperationType == OperationType.Create ||
                info.OperationType == OperationType.Update ||
                info.OperationType == OperationType.Delete))
            {
                var name = DocsWhere("Users", info.AttrPath)
                           .Value("{'Name':$}");

                return name == Context.User.Name || Context.User.HasRole("Admin");
            }
            else if (info.Variable == "MyChannel" &&
                    (info.OperationType == OperationType.Create ||
                     info.OperationType == OperationType.Update ||
                     info.OperationType == OperationType.Delete))
            {
                var name = DocsWhere("Channels", info.AttrPath)
                           .Value("{'Owner':$}");

                return name == Context.User.Name || Context.User.HasRole("Admin");
            }
            else if (info.OperationType == OperationType.Read)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ClearHistory()
        {
            MessageBox("Are you sure to clear history?",
                       "Clear history",
                       MessageBoxButtonType.YesNo,
                       onSave: result =>
                       {
                           DocsWhere("Users", "{'Name':@UserName}")
                           .Delete("{'History':[$]}");

                           Dashboard();
                       });

            return true;
        }

        private bool NewVideo(EventInfo info)
        {
            var docID = DocsWhere("Channels", info.AttrPath)
                        .GetFirstID();

            CreateNewDocForArray("NewVideo", "Channels", "{'Videos':[$]}", docID)
            .OpenForm();

            return true;
        }

        private bool Subscribe(EventInfo info)
        {
            var channel = DocsWhere("Channels", info.AttrPath)
                          .Value("{'Name':$}");

            DocsWhere("Users", "{'Name':@UserName}")
            .Update("{'Subscribes':[Add,@Channel]}", channel);

            MessageBox("Thank you. You are subscribed on the channel.", MessageBoxButtonType.Ok);

            return true;
        }

        private bool Unsubscribe(EventInfo info)
        {
            var channel = DocsWhere("Channels", info.AttrPath)
                          .Value("{'Name':$}");

            DocsWhere("Users", "{'Name':@UserName}")
            .Delete("{'Subscribes':[@Channel]}", channel);

            MessageBox("You are unsubscribed from the channel.", MessageBoxButtonType.Ok);

            return true;
        }

        private bool NewComment(EventInfo info)
        {
            var uid = info.FindFirstValue("UID");
            var comment = info.FindFirstValue("Comment");

            DocsWhere("Channels", "{'Videos':[{'UID':@UID}]}", uid)
                .Update("{'Videos':[{'Comments':[Add,{'Who':@UserName,'OnDate':@Now,'Avatar':@Avatar,'Text':@Comment}]}]}",
                         Context.User.GetDefaultAvatar("Guest.png"), comment);

            OpenVideo(uid);

            return true;
        }

        private bool NewLike(EventInfo info)
        {
            var uid = info.FindFirstValue("UID");

            if (!DocsWhere("Channels", "{'Videos':[{'UID':@UID,'Likes':[Any,@UserName]}]}", uid).Any())
            {
                DocsWhere("Channels", "{'Videos':[{'UID':@UID}]}", uid)
                    .Update("{'Videos':[{'Likes':[Add,@UserName]}]}");
            }

            OpenVideo(uid);

            return true;
        }

        private bool FilterVideos(EventInfo info)
        {
            Dashboard(info.FindFirstValue("FilterText"));

            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Login"        => Login(),
                "Logout"       => Logout(),
                "Register"     => Register(),
                "MyUser"       => DocsWhere("Users", "{'Name':@UserName}").OpenForm(),
                "Users"        => DocsOf("Users").OpenForm(),
                "NewChannel"   => CreateNewDocFor("NewChannel", "Channels").OpenForm(onSave: result => MessageBox("Thank you ! New channel created.")),
                "MyChannels"   => DocsWhere("Channels", "{'Owner':@UserName,'IsLocked':false}").OpenForm(),
                "Channels"     => DocsWhere("Channels", "{'IsLocked':false}").OpenForm(),
                "History"      => DocsWhere("Users", "{'Name':@UserName}").ExtendUIDimension("{'ReadOnly':true,'History':{'Visible':true,'UID':{'Visible':false}}}").OpenForm("{'History':[$]}"),
                "ClearHistory" => ClearHistory(),
                "NewVideo"     => NewVideo(info),
                "Subscribe"    => Subscribe(info),
                "Unsubscribe"  => Unsubscribe(info),
                "NewComment"   => NewComment(info),
                "NewLike"      => NewLike(info),
                "Filter"       => FilterVideos(info),
                _ => base.OnEventDimension(info)
            };

        public override void OnStart()
        {
            TryAutoLogin();

            if (!Context.HasUrlTag)
            {
                Dashboard();
            }
            else
            {
                OpenVideo(Context.UrlTag);
            }
        }
    }
}
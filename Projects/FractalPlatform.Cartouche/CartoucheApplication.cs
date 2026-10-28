using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Database.Engine;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Clients;
using FractalPlatform.Common.Enums;

namespace FractalPlatform.Cartouche
{
    public class CartoucheApplication : DashboardApplication
    {
        public class Comment
        {
            public string Name
            {
                get;
                set;
            }
            public string Text
            {
                get;
                set;
            }
            public DateTime OnDate
            {
                get;
                set;
            }
            public List<string> Likes
            {
                get;
                set;
            }
        }

        public class Post
        {
            public string Name
            {
                get;
                set;
            }
            public string FullName
            {
                get;
                set;
            }
            public string Avatar
            {
                get;
                set;
            }
            public string Text
            {
                get;
                set;
            }
            public string Picture
            {
                get;
                set;
            }
            public DateTime OnDate
            {
                get;
                set;
            }

            public string LikePost
            {
                get;
                set;
            }

            public string EditPost
            {
                get;
                set;
            }

            public List<string> Likes
            {
                get;
                set;
            }
            public List<Comment> Comments
            {
                get;
                set;
            }
        }

        private class Bot
        {
            public string Name
            {
                get;
                set;
            }

            public string FullName
            {
                get;
                set;
            }

            public string Avatar
            {
                get;
                set;
            }

            public string Prompt
            {
                get;
                set;
            }
        }

        private bool NewPost()
        {
            CreateNewDocFor("NewPost", "Posts")
                        .OpenForm(onSave: result => ProcessBots(result.TargetDocID),
                                  onClose: result => Dashboard());
            return true;
        }

        private bool OpenPost(uint docID)
        {
            DocsWhere("Posts", docID)
                .OpenForm(onClose: result => Dashboard());
            return true;
        }

        private bool Dashboard()
        {
            //var following = DocsWhere("Users", "{'Name':@UserName}")
            //    .Values("{'Following':[$]}");

            //following.Add(Context.User.Name);

            var pageSize = 30U;

            var lastDocID = DocsOf("Posts").GetLastID();

            var skip = 0U;

            if (lastDocID > pageSize)
            {
                skip = lastDocID - pageSize;
            }

            var page = 0U;

            if (Context.HasUrlTag)
            {
                page = uint.Parse(Context.UrlTag) - 1;
            }

            if (skip >= page * pageSize)
            {
                skip -= page * pageSize;
            }

            //var posts = DocsWhere("Posts", "{'Name':Any(@Following)}", following)
            var posts = DocsOf("Posts")
                .Skip(skip)
                .Take(pageSize)
                .Select<Post>()
                .Select(x => new
                {
                    Name = x.Name,
                    FullName = x.FullName,
                    Text = x.Text,
                    Picture = x.Picture,
                    Avatar = x.Avatar,
                    OnDate = x.OnDate,
                    LikePost = x.LikePost,
                    EditPost = x.Name == Context.User.Name ? "Edit Post" : null,
                    Likes = x.Likes.Count,
                    Comments = x.Comments.Count
                })
                .Reverse()
                .ToList();

            if (!posts.Any())
            {
                NewPost();

                return true;
            }

            var values = DocsWhere("Users", "{'Name':@UserName}")
                            .Values("{'FullName':$,'Avatar':$}");

            FirstDocOf("Dashboard")
                .ToCollection()
                .ExtendDocument("{'FullName':@FullName,'Avatar':@Avatar}", values[0], values[1])
                .SetToArrayPath(posts, "Posts", Constants.FIRST_DOC_ID, true)
                .OpenForm();

            return true;
        }

        public override void OnStart()
        {
            if (Context.UrlTag == "settings")
            {
                FirstDocOf("Settings")
                    .OpenForm();
            }
            else
            {
                base.OnStart();
            }
        }

        public override void OnRegister(FormResult result) => TryAutoLogin();

        public override void OnLogin(FormResult result) => Dashboard();

        private bool RegisterUser()
        {
            CreateNewDocFor("NewUser", "Users")
                .OpenForm(onSave: result => MessageBox("You are registered !", MessageBoxButtonType.Ok, result => Login()),
                          onCancel: result => Login());
            return true;
        }

        private bool Follow(EventInfo info)
        {
            var name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            DocsWhere("Users", "{'Name':@UserName}").Update("{'Following':[Add,@Name]}", name);
            return true;
        }

        private bool FollowUser(EventInfo info)
        {
            var name = DocsWhere("Users", info.AttrPath).Value("{'Name':$}");
            if (!DocsWhere("Users", "{'Name':@UserName,'Following':[Any,@name]}", name).Any())
            {
                DocsWhere("Users", "{'Name':@UserName}").Update("{'Following':[Add,@Name]}", name);
            }
            return true;
        }

        private bool Unfollow(EventInfo info)
        {
            var name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            DocsWhere("Users", "{'Name':@UserName}").Delete("{'Following':[@Name]}", name);
            return true;
        }

        private bool LikeComment(EventInfo info)
        {
            var query = DocsWhere("Posts", info.AttrPath)
                            .AndWhere("{'Comments':[{'Likes':[Any,@UserName]}]}");

            if (!query.Any())
                DocsWhere("Posts", info.AttrPath).Update("{'Comments':[{'Likes':[Add,@UserName]}]}");
            else
                DocsWhere("Posts", info.AttrPath).Delete("{'Comments':[{'Likes':[@UserName]}]}");

            OpenPost(info.DocID);
            return true;
        }

        private bool ReplyComment(EventInfo info)
        {
            var name = DocsWhere("Posts", info.AttrPath).Value("{'Comments':[{'Name':$}]}");

            CreateNewDocForArray("NewComment", "Posts", "{'Comments':[$]}", info.DocID)
                .ExtendDocument("{'Text':@Text}", name)
                .OpenForm();
            return true;
        }

        private bool LikePost(EventInfo info)
        {
            if (info.Collection.Name == "Dashboard")
            {
                var nameAndOnDate = info.Collection
                                        .GetWhere(info.AttrPath)
                                        .Values("{'Posts':[{'Name':$,'OnDate':$}]}");

                var docID = DocsWhere("Posts", "{'Name':@Name,'OnDate':@OnDate}", nameAndOnDate[0], nameAndOnDate[1])
                                .GetFirstID();

                var query = DocsWhere("Posts", docID).AndWhere("{'Likes':[Any,@UserName]}");

                if (!query.Any())
                    DocsWhere("Posts", docID).Update("{'Likes':[Add,@UserName]}");
                else
                    DocsWhere("Posts", docID).Delete("{'Likes':[@UserName]}");

                Dashboard();
            }
            else
            {
                var query = DocsWhere("Posts", info.DocID).AndWhere("{'Likes':[Any,@UserName]}");

                if (!query.Any())
                    DocsWhere("Posts", info.DocID).Update("{'Likes':[Add,@UserName]}");
                else
                    DocsWhere("Posts", info.DocID).Delete("{'Likes':[@UserName]}");

                OpenPost(info.DocID);
            }

            return true;
        }

        private bool EditPost(EventInfo info)
        {
            uint docID;

            if (info.Collection.Name == "Dashboard")
            {
                var nameAndOnDate = info.Collection
                                        .GetWhere(info.AttrPath)
                                        .Values("{'Posts':[{'Name':$,'OnDate':$}]}");

                docID = DocsWhere("Posts", "{'Name':@Name,'OnDate':@OnDate}", nameAndOnDate[0], nameAndOnDate[1])
                        .GetFirstID();

                DocsWhere("Posts", docID)
                    .ExtendUIDimension("{'Style':'Save:Update','IsRawPage':false,'Layout':'','Visible':false,'Text':{'Visible':true},'Picture':{'Visible':true}}")
                    .OpenForm(onClose: result => Dashboard());
            }
            else
            {
                docID = info.DocID;

                DocsWhere("Posts", docID)
                    .ExtendUIDimension("{'Style':'Save:Update','IsRawPage':false,'Layout':'','Visible':false,'Text':{'Visible':true},'Picture':{'Visible':true}}")
                    .OpenForm();
            }

            DocsWhere("Posts", docID).Update("{'OnDate':@Now}");

            return true;
        }

        private bool EditComment(EventInfo info)
        {
            var text = DocsWhere("Posts", info.AttrPath)
                        .Values("{'Comments':[{'Text':$}]}");

            FirstDocOf("NewComment")
                .ExtendDocument("{'Text':@Text}", text)
                .ExtendUIDimension("{'Style':'CollLabel:Update comment;Save:Update'}")
                .OpenForm(onSave: result =>
                {
                    var text = result.FindFirstValue("Text");
                    var picture = result.FindFirstValue("Picture");

                    if (string.IsNullOrEmpty(picture))
                    {
                        DocsWhere("Posts", info.AttrPath)
                            .Update("{'Comments':[{'Text':@Text}]}", text);
                    }
                    else
                    {
                        DocsWhere("Posts", info.AttrPath)
                            .Update("{'Comments':[{'Text':@Text,'Picture':@Picture}]}", text, picture);
                    }

                    result.NeedReloadData = true;
                });

            return true;
        }

        private bool OpenComments(EventInfo info)
        {
            var nameAndOnDate = info.Collection
                                    .GetWhere(info.AttrPath)
                                    .Values("{'Posts':[{'Name':$,'OnDate':$}]}");

            var docID = DocsWhere("Posts", "{'Name':@Name,'OnDate':@OnDate}", nameAndOnDate[0], nameAndOnDate[1])
                            .GetFirstID();

            DocsWhere("Posts", docID).OpenForm(onClose: result => Dashboard());

            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Register"    => RegisterUser(),
                "EditUser"    => DocsWhere("Users", "{'Name':@UserName}").OpenForm(onSave: result => Context.User.Avatar = result.FindFirstValue("Avatar")),
                "SignOut"      => Logout(),
                "NewPost"     => NewPost(),
                "EditBots"    => DocsWhere("Users", "{'IsBot':true}").SetDimension(DimensionType.Filter, "{}").ExtendUIDimension("{'Settings':{'Visible':true},'Following':{'Visible':true,'ReadOnly':true}}").OpenForm(),
                "NewBot"      => CreateNewDocFor("NewUser", "Users").ExtendDocument("{'IsBot':true}").ExtendUIDimension("{'Style':'Save:Create;CollLabel:New bot','Category':{'Visible':true},'Prompt':{'Visible':true},'Settings':{'Visible':true},'Password':{'Visible':false}}").OpenForm(onSave: result => MessageBox("You are added new bot !", MessageBoxButtonType.Ok, result => Dashboard()), onCancel: result => Dashboard()),
                "NewComment"  => CreateNewDocForArray("NewComment", "Posts", "{'Comments':[$]}", info.DocID).OpenForm(),
                "Follow"      => Follow(info),
                "FollowUser"  => FollowUser(info),
                "Unfollow"    => Unfollow(info),
                "LikeComment" => LikeComment(info),
                "Reply"       => ReplyComment(info),
                "LikePost"    => LikePost(info),
                "EditPost"    => EditPost(info),
                "EditComment" => EditComment(info),
                "Comments"    => OpenComments(info),
                _ => base.OnEventDimension(info)
            };

        private bool SecurityFollow(SecurityInfo info)
        {
            var name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            return (!DocsWhere("Users", "{'Name':@UserName}").AndWhere("{'Following':[Any,@Name]}", name).Any()) && Context.User.Name != name;
        }

        private bool SecurityUnfollow(SecurityInfo info)
        {
            var name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            return DocsWhere("Users", "{'Name':@UserName}").AndWhere("{'Following':[Any,@Name]}", name).Any() && Context.User.Name != name;
        }

        public override bool OnSecurityDimension(SecurityInfo info) =>
            info.Variable switch
            {
                "Follow"   => SecurityFollow(info),
                "Unfollow" => SecurityUnfollow(info),
                "EditPost" => DocsWhere("Posts", info.AttrPath).Value("{'Name':$}") == Context.User.Name,
                _ => base.OnSecurityDimension(info)
            };

        public void ProcessBots(uint docID)
        {
            return;

            var text = DocsWhere("Posts", docID)
                        .Value("{'Text':$}");

            var random = new Random();
            int count = random.Next(2, 4);

            var bots = DocsWhere("Users", "{'IsBot':true,'IsActive':true}")
                        .Select<Bot>()
                        .OrderBy(x => Guid.NewGuid())
                        .Take(count);

            foreach (var bot in bots)
            {
                var prompt = $"Ты {bot.Prompt}. Сгенерируй короткое сообщение до 400 символов как ответ на это сообщение {text}";

                var aiText = AI.Generate(prompt, AIModel.GPT4oMini).Text;

                Log("Processed bot => " + bot.Name);
                Log("Message => " + aiText);

                DocsWhere("Posts", docID)
                    .Update("{'Comments':[Add,@Comment]}",
                        new
                        {
                            Name = bot.Name,
                            Avatar = bot.Avatar,
                            FullName = bot.FullName,
                            Text = aiText,
                            OnDate = DateTime.Now
                        });
            }
        }

        private object ComputeText(ComputedInfo info)
        {
            var text = System.Net.WebUtility.HtmlEncode(info.AttrValue.ToString());

            text = text.Replace(((char)13).ToString() + ((char)10).ToString(), "<br>")
                       .Replace(((char)10).ToString(), "<br>");

            string picture;

            if (info.Collection.Name == "Dashboard")
            {
                picture = info.Collection
                              .GetWhere(info.AttrPath)
                              .Value("{'Posts':[{'Picture':$}]}");
            }
            else if (info.AttrPath.FirstPath != "Comments")
            {
                picture = info.Collection
                              .GetWhere(info.AttrPath)
                              .Value("{'Picture':$}");
            }
            else
            {
                picture = info.Collection
                              .GetWhere(info.AttrPath)
                              .Value("{'Comments':[{'Picture':$}]}");
            }

            if (!string.IsNullOrEmpty(picture) && picture != "null")
            {
                text += $"<br><br><img style='max-width:400px; max-height:300px' src='{GetFileUrl(picture)}'/>";
            }

            return text;
        }

        private object ComputeOnDate(ComputedInfo info)
        {
            var dt = info.AttrValue.ToString();

            var culture = CultureInfo.InvariantCulture;
            var ts = DateTime.Now.Subtract(DateTime.Parse(dt, culture));

            if ((int)ts.TotalDays / 365 > 0) return "год назад";
            if ((int)ts.TotalDays / 30 > 0) return $"{(int)ts.TotalDays / 30} месяцев назад";
            if ((int)ts.TotalDays > 0) return $"{(int)ts.TotalDays} дней назад";
            if ((int)ts.TotalHours > 0) return $"{(int)ts.TotalHours} часов назад";
            if ((int)ts.TotalMinutes > 0) return $"{(int)ts.TotalMinutes} минут назад";
            return $"{(int)ts.TotalSeconds} секунд назад";
        }

        private object ComputeAvatar(ComputedInfo info)
        {
            string name;

            if (info.AttrPath.Count == 1)
                name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            else
                name = DocsWhere("Posts", info.AttrPath).Value("{'Comments':[{'Name':$}]}");

            return DocsWhere("Users", "{'Name':@Name}", name).Value("{'Avatar':$}");
        }

        private object ComputeFullName(ComputedInfo info)
        {
            string name;

            if (info.AttrPath.Count == 1)
                name = DocsWhere("Posts", info.AttrPath).Value("{'Name':$}");
            else
                name = DocsWhere("Posts", info.AttrPath).Value("{'Comments':[{'Name':$}]}");

            return DocsWhere("Users", "{'Name':@Name}", name).Value("{'FullName':$}");
        }

        private object ComputePagination(ComputedInfo info)
        {
            var activePage = 1U;

            if (Context.HasUrlTag)
            {
                activePage = uint.Parse(Context.UrlTag);
            }

            var sb = new StringBuilder();

            uint currPage;

            var startPage = (activePage - 1) / 10 * 10 + 1;

            if (startPage > 1)
            {
                sb.AppendLine($"<a href='{Context.InstanceUrl}/{Name}/?tag={startPage - 1}' class='pagination-button pagination-next'>Prev</a>");
            }

            for (currPage = startPage; currPage < startPage + 10; currPage++)
            {
                if (activePage == currPage)
                {
                    sb.AppendLine($"<a href='{Context.InstanceUrl}/{Name}/?tag={currPage}' class='pagination-button active'>{currPage}</a>");
                }
                else
                {
                    sb.AppendLine($"<a href='{Context.InstanceUrl}/{Name}/?tag={currPage}' class='pagination-button'>{currPage}</a>");
                }
            }

            sb.AppendLine($"<a href='{Context.InstanceUrl}/{Name}/?tag={currPage}' class='pagination-button pagination-next'>Next</a>");

            return sb.ToString();
        }

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "ValidatePostName"    => DocsWhere("Users", "{'Name':@Name}", info.AttrValue).Any(),
                "ValidateCommentName" => DocsWhere("Users", "{'Name':@Name}", info.AttrValue).Any(),
                "ValidateLikePost"    => DocsWhere("Users", "{'Name':@Name}", info.AttrValue).Any() &&
                                         !DocsWhere("Posts", info.AttrPath.DocID).AndWhere("{'Likes':[Any,@Name]}", info.AttrValue).Any(),
                "ValidateFollowUser"  => DocsWhere("Users", "{'Name':@Name}", info.AttrValue).Any() &&
                                         !DocsWhere("Users", info.AttrPath.DocID).AndWhere("{'Following':[Any,@Name]}", info.AttrValue).Any(),
                "Text"                => ComputeText(info),
                "OnDate"              => ComputeOnDate(info),
                "Avatar"              => ComputeAvatar(info),
                "EditComment"         => DocsWhere("Posts", info.AttrPath).Value("{'Comments':[{'Name':$}]}") == User.Name ? "Edit Comment" : null,
                "FullName"            => ComputeFullName(info),
                "Pagination"          => ComputePagination(info),
                _ => base.OnComputedDimension(info)
            };
    }
}
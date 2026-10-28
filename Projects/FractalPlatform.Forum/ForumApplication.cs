using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine.Info;
using System.Text.RegularExpressions;
using FractalPlatform.Database.Engine;

namespace FractalPlatform.Forum
{
    public class ForumApplication : DashboardApplication
    {
        private string _category;

        private uint _topicID;

        private void Dashboard()
        {
            var categories = DocsOf("Categories").ToStorage("{'Title':$,'Description':$,'CountMessages':$,'CountTopics':$,'LastMessage':{'Who':$,'OnDate':$}}");

            FirstDocOf("Dashboard")
                .ToCollection()
                .SetToArrayPath(categories, "Categories")
                .OpenForm();
        }

        private void CategoryDashboard()
        {
            var topics = DocsWhere("Topics", "{'Category':@Category}", _category).ToStorage();

            FirstDocOf("CategoryDashboard")
                .ToCollection()
                .SetToArrayPath(topics, "Topics", Constants.FIRST_DOC_ID, true)
                .OpenForm(onClose: result => Dashboard());
        }

        private void TopicDashboard()
        {
            DocsWhere("Topics", _topicID).Update("{'CountViews':Add(1)}");

            var topic = DocsWhere("Topics", _topicID).ToStorage();

            FirstDocOf("TopicDashboard")
                  .ToCollection()
                  .RemovePartDocument("{'Messages':$}")
                  .MergeToPath(topic, _topicID)
                  .OpenForm(onClose: result => CategoryDashboard());
        }

        public override bool OnOpenForm(FormInfo info)
        {
            if (info.AttrPath.FirstPath == "Categories")
            {
                _category = info.Collection
                                   .GetWhere(info.AttrPath)
                                   .Value("{'Categories':[{'Title':$}]}");

                CategoryDashboard();

                return false;
            }
            else if (info.AttrPath.FirstPath == "Topics")
            {
                _topicID = info.Collection
                                   .GetWhere(info.AttrPath)
                                   .UIntValue("{'Topics':[{'DocID':$}]}");

                TopicDashboard();

                return false;
            }
            else
            {
                return true;
            }
        }

        private string ReplaceUrls(string message)
        {
            var matches = Regex.Matches(message, "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)");

            foreach (Match match in matches)
            {
                message = message.Replace(match.Value, $"<a href=\"{match.Value}\">{match.Value}</a>");
            }

            return message;
        }

        private object DescriptionShort(ComputedInfo info)
        {
            var description = info.Collection
                                          .GetWhere(info.AttrPath)
                                          .Value("{'Description':$}") ?? string.Empty;

            return description.Contains("\n") ? description.Substring(0, description.IndexOf("\n")) : description;
        }

        private object DescriptionHtml(ComputedInfo info)
        {
            var description = info.Collection
                                          .GetWhere(info.AttrPath)
                                          .Value("{'Description':$}") ?? string.Empty;

            description = description.Replace("\n", "<br>");

            description = ReplaceUrls(description);

            return description;
        }

        private object MessageHtml(ComputedInfo info)
        {
            var message = info.Collection
                                      .GetWhere(info.AttrPath)
                                      .Value("{'Messages':[{'Message':$}]}") ?? string.Empty;

            message = message.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br>");

            message = ReplaceUrls(message);

            return Regex.Replace(message, "\\[QUOTE=(?<Name>[a-zA-Z0-9]+)\\]", m => $"<div style='border-style:ridge;'>{m.Groups["Name"].Value} writes:<br/>")
                        .Replace("[/QUOTE]", "</div>");
        }

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "WhoCountMessages"        => DocsWhere("Users", "{'Name':@Name}", info.Collection.GetWhere(info.AttrPath).Value("{'Who':{'Name':$}}")).Value("{'CountMessages':$}"),
                "WhoRegistered"           => DocsWhere("Users", "{'Name':@Name}", info.Collection.GetWhere(info.AttrPath).Value("{'Who':{'Name':$}}")).Value("{'Registered':$}"),
                "UserMessageCountMessages" => DocsWhere("Users", "{'Name':@Name}", info.Collection.GetWhere(info.AttrPath).Value("{'Messages':[{'Who':$}]}")).Value("{'CountMessages':$}"),
                "UserMessageRegistered"   => DocsWhere("Users", "{'Name':@Name}", info.Collection.GetWhere(info.AttrPath).Value("{'Messages':[{'Who':$}]}")).Value("{'Registered':$}"),
                "UserCountMessages"       => DocsWhere("Topics", "{'Messages':[{'Who':@User}]}", info.Collection.GetWhere(info.AttrPath).Value("{'Name':$}")).Count("{'Messages':[{'Who':$}]}"),
                "Category"                => _category,
                "Title"                   => info.Collection.GetWhere(info.AttrPath).Value("{'Title':$}"),
                "DescriptionShort"        => DescriptionShort(info),
                "DescriptionHtml"         => DescriptionHtml(info),
                "MessageHtml"             => MessageHtml(info),
                _ => base.OnComputedDimension(info)
            };

        private bool LoginButton(EventInfo info)
        {
            var loginAndPass = info.Collection
                                        .Values("{'Login':$,'Password':$}");

            if (TryLogin(loginAndPass[0], loginAndPass[1]))
            {
                if (info.Collection.Name == "CategoryDashboard")
                    CategoryDashboard();
                else if (info.Collection.Name == "TopicDashboard")
                    TopicDashboard();
                else
                    Dashboard();
            }
            else
            {
                MessageBox("Wrong credentials");
            }

            return true;
        }

        private bool NewTopic()
        {
            CreateNewDocFor("NewTopic", "Topics")
                  .OpenForm(onSave: result =>
                  {
                      Client.SetDefaultCollection("Categories")
                            .GetWhere("{'Title':@Title}", _category)
                            .Update("{'LastMessage':{'Who':@UserName,'OnDate':@Now},'CountTopics':Add(1)}");

                      CategoryDashboard();
                  });

            return true;
        }

        private bool NewMessage()
        {
            CreateNewDocForArray("NewMessage", "Topics", "{'Messages':[$]}", _topicID)
                  .OpenForm(onSave: result =>
                  {
                      Client.SetDefaultCollection("Categories")
                            .GetWhere("{'Title':@Title}", _category)
                            .Update("{'LastMessage':{'Who':@UserName,'OnDate':@Now},'CountMessages':Add(1)}");

                      Client.SetDefaultCollection("Topics")
                            .GetDoc(_topicID)
                            .Update("{'LastMessage':{'Who':@UserName,'OnDate':@Now},'CountMessages':Add(1)}");

                      TopicDashboard();
                  });

            return true;
        }

        private bool NewQuoteMessage(EventInfo info)
        {
            var whoAndMessage = info.Collection
                                         .GetWhere(info.AttrPath)
                                         .Values("{'Messages':[{'Who':$,'Message':$}]}");

            CreateNewDocForArray("NewMessage", "Topics", "{'Messages':[$]}", _topicID)
                  .ExtendDocument("{'Message':@Message}", $"[QUOTE={whoAndMessage[0]}]{whoAndMessage[1]}[/QUOTE]")
                  .OpenForm(onClose: result => TopicDashboard());

            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Users"          => DocsOf("Users").OpenForm(),
                "EditCategories" => DocsOf("Categories").OpenForm(onClose: result => Dashboard()),
                "EditTopics"     => DocsWhere("Topics", "{'Category':@Category}", _category).OpenForm(onClose: result => CategoryDashboard()),
                "EditTopic"      => DocsWhere("Topics", _topicID).OpenForm(onClose: result => TopicDashboard()),
                "Register"       => Register(),
                "LoginButton"    => LoginButton(info),
                "NewTopic"       => NewTopic(),
                "NewMessage"     => NewMessage(),
                "NewQuoteMessage" => NewQuoteMessage(info),
                "Who" or "Name"  => DocsWhere("Users", "{'Name':@Name}", info.AttrValue).OpenForm(),
                _ => base.OnEventDimension(info)
            };

        public override void OnStart()
        {
            TryAutoLogin();

            Dashboard();
        }
    }
}
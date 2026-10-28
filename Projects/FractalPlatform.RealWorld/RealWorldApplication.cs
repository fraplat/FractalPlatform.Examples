using System;
using System.Linq;
using System.Collections.Generic;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Common.Enums;

namespace FractalPlatform.RealWorld
{
    public class RealWorldApplication : DashboardApplication
    {
        private string _dashboardTag;
        private string _profileTag;

        private class Article
        {
            public string Who { get; set; }
            public string Avatar { get; set; }
            public DateTime OnDate { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public List<string> Tags { get; set; }
            public List<string> Likes { get; set; }
            public int CountLikes => Likes.Count;
        }

        private void OpenArticle(Collection collection, AttrPath attrPath)
        {
            var title = collection.GetWhere(attrPath)
                                  .Value(DQL("{@FirstPath:[{'Title':$}]}", attrPath.FirstPath));

            DocsWhere("Articles", "{'Title':@Title}", title).OpenForm();
        }

        public override bool OnOpenForm(FormInfo info)
        {
            if (info.AttrPath.FirstPath == "Posts")
            {
                OpenArticle(info.Collection, info.AttrPath);

                return false;
            }
            else if (info.AttrPath.FirstPath == "PopularTags")
            {
                var tag = info.Collection
                              .GetWhere(info.AttrPath)
                              .Value("{'PopularTags':[$]}");

                _dashboardTag = tag;

                Dashboard();

                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool OnSecurityDimension(SecurityInfo info)
        {
            if (Context.User.IsGuest)
                return false;
            
            if (info.OperationType != OperationType.Read)
                return true;
            
            var who = info.Collection.GetDoc(info.DocID).Value("{'Who':$}");

            if (string.IsNullOrEmpty(who))
                return true;
            
            return info.Variable switch
            {
                "FollowUser" => !DocsWhere("Users", "{'Name':@Who,'Followers':[Any,@UserName]}", who)
                                    .Any() && who != Context.User.Name,
                "UnfollowUser" => DocsWhere("Users", "{'Name':@Who,'Followers':[Any,@UserName]}", who)
                                    .Any() && who != Context.User.Name,
                "AddLike" => !DocsWhere("Articles", info.DocID)
                                 .AndWhere("{'Likes':[Any,@UserName]}")
                                 .Any() && who != Context.User.Name,
                "RemoveLike" => DocsWhere("Articles", info.DocID)
                                    .AndWhere("{'Likes':[Any,@UserName]}")
                                    .Any() && who != Context.User.Name,
                "RemoveComment" => DocsWhere("Articles", info.AttrPath)
                                        .Value("{'Comments':[{'Who':$}]}") == Context.User.Name,
                "EditArticle" => who == Context.User.Name,
                "RemoveArticle" => who == Context.User.Name,
                _ => true
            };
        }

        private object CountFollowers(ComputedInfo info)
        {
            var who = info.Collection.GetDoc(info.DocID).Value("{'Who':$}");

            return DocsWhere("Users", "{'Name':@Name}", who).Count("{'Followers':[$]}");
        }

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "CountLikes"      => DocsWhere("Articles", info.DocID).Count("{'Likes':[$]}"),
                "CountFollowers"  => CountFollowers(info),
                "GlobalFeedActive" => string.IsNullOrEmpty(_dashboardTag) || _dashboardTag == "Global" ? "active" : "",
                "YourFeedActive"   => _dashboardTag == "Your" ? "active" : "",
                "MyPostsActive"    => string.IsNullOrEmpty(_profileTag) || _profileTag == "MyPosts" ? "active" : "",
                "LikedPostsActive" => _profileTag == "LikedPosts" ? "active" : "",
                "TagFeedVisible"   => string.IsNullOrEmpty(_dashboardTag) || _dashboardTag == "Global" || _dashboardTag == "Your" ? "style='display:none'" : "",
                "TagFeedValue"     => _dashboardTag,
                _ => base.OnComputedDimension(info)
            };

        public override void OnLogin(FormResult result) => Dashboard();

        private bool FollowUser(EventInfo info) =>
            DocsWhere("Users", "{'Name':@Who}", info.FindFirstValue("Who"))
                .Update("{'Followers':[Add,@UserName]}");

        private bool UnfollowUser(EventInfo info) =>
            DocsWhere("Users", "{'Name':@Who}", info.FindFirstValue("Who"))
                .Delete("{'Followers':[@UserName]}");

        private bool AddComment(EventInfo info) =>
            DocsWhere("Articles", info.DocID)
                .Update("{'Comments':[Add,{'Who':@UserName,'Avatar':@UserAvatar,'OnDate':@Now,'Text':@Text,'RemoveComment':''}]}", info.FindFirstValue("Comment"));

        private bool GlobalFeed()
        {
            _dashboardTag = "Global";
            return Dashboard();
        }

        private bool YourFeed()
        {
            _dashboardTag = "Your";
            return Dashboard();
        }

        private bool ViewWho(EventInfo info)
        {
            Context.UrlTag = info.AttrValue.ToString();
            _profileTag = "MyPosts";
            return Profile();
        }

        private bool MyPosts()
        {
            _profileTag = "MyPosts";
            return Profile();
        }

        private bool LikedPosts()
        {
            _profileTag = "LikedPosts";
            return Profile();
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "Register"      => Register(),
                "Login"         => Login(),
                "Logout"        => Logout(),
                "AddArticle"    => CreateNewDocFor("Post", "Articles").OpenForm(onClose: result => Dashboard()),
                "Settings"      => DocsWhere("Users", "{'Name':@UserName}").OpenForm(),
                "AddLike"       => DocsWhere("Articles", info.DocID).Update("{'Likes':[Add,@UserName]}"),
                "RemoveLike"    => DocsWhere("Articles", info.DocID).Delete("{'Likes':[@UserName]}"),
                "FollowUser"    => FollowUser(info),
                "UnfollowUser"  => UnfollowUser(info),
                "AddComment"    => AddComment(info),
                "RemoveComment" => DocsWhere("Articles", info.AttrPath).Delete("{'Comments':[$]}"),
                "EditArticle"   => DocsWhere("Articles", info.DocID).ExtendUIDimension("{'Layout':'Post'}").OpenForm(onClose: result => OpenArticle(info.Collection, info.AttrPath)),
                "RemoveArticle" => DocsWhere("Articles", info.DocID).Remove(),
                "GlobalFeed"    => GlobalFeed(),
                "YourFeed"      => YourFeed(),
                "Who"           => ViewWho(info),
                "MyPosts"       => MyPosts(),
                "LikedPosts"    => LikedPosts(),
                _ => base.OnEventDimension(info)
            };

        private bool Profile()
        {
            var userName = Context.UrlTag;

            var posts = _profileTag == "MyPosts" ? DocsWhere("Articles", "{'Who':@Name}", userName).ToStorage() :
                                                   DocsWhere("Articles", "{'Likes':[Any,@Name]}", userName).ToStorage();

            var user = DocsWhere("Users", "{'Name':@Name}", userName).ToStorage();

            FirstDocOf("Profile")
                    .ToCollection()
                    .ExtendDocument("{'Who':@Who}", userName)
                    .ExtendDocument(user.ToJson())
                    .MergeToArrayPath(posts, "Posts")
                    .SetDimension(DimensionType.Pagination, "{'Posts':{'Page':{'Size':10}}}")
                    .OpenForm();

            return true;
        }

        private bool Dashboard()
        {
            var globalFeed = DocsOf("Articles")
                                    .Select<Article>()
                                    .OrderByDescending(x => x.OnDate);

            IEnumerable<Article> posts;

            if (string.IsNullOrEmpty(_dashboardTag) || _dashboardTag == "Global")
            {
                posts = globalFeed;
            }
            else if (_dashboardTag == "Your")
            {
                var followUsers = DocsWhere("Users", "{'Followers':[Any,@UserName]}").Values("{'Name':$}");

                posts = globalFeed.Where(x => followUsers.Any(y => y == x.Who));
            }
            else //tag
            {
                posts = globalFeed.Where(x => x.Tags.Contains(_dashboardTag));
            }

            var allTags = globalFeed.SelectMany(x => x.Tags);

            var popularTags = allTags.Distinct()
                                     .Select(x => new
                                     {
                                         Tag = x,
                                         Count = allTags.Count(y => y == x)
                                     })
                                     .OrderByDescending(x => x.Count)
                                     .Take(10)
                                     .Select(x => x.Tag);

            FirstDocOf("Dashboard")
                    .ToCollection()
                    .MergeToArrayPath(posts, "Posts")
                    .MergeToArrayPath(popularTags, "PopularTags")
                    .SetDimension(DimensionType.Pagination, "{'Posts':{'Page':{'Size':10}}}")
                    .OpenForm();

            return true;
        }

        public override void OnStart()
        {
            TryAutoLogin();

            _dashboardTag = Context.UrlTag;

            Dashboard();
        }
    }
}
using System.Linq;
using System;
using FractalPlatform.Database.Engine;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Client.App;
using FractalPlatform.Common.Enums;
using FractalPlatform.Common.Clients;

namespace FractalPlatform.LearnDictionary
{
    public class LearnDictionaryApplication : BaseApplication
    {
        public override void OnStart() =>
            UsePassword("777", () => FirstDocOf("Dashboard").OpenForm());

        private bool LearnText(EventInfo info)
        {
            var word = DocsWhere("Words", info.AttrPath).Value("{'Words':[{'Word':$}]}");

            var aboutMe = FirstDocOf("MyLife").Value("{'AboutMe':$}");

            var answer = AI.Generate($"Create a micro text with 3-4 simple sentences with phrase: {word}. These sentences should be imagined about me and my life: {aboutMe}", AIModel.GPT4oMini);
            var text = answer.Text;

            //test

            text = text.Replace(word, $"<b>{word}</b>");

            MessageBox(text, "About my life", MessageBoxButtonType.Ok);

            return true;
        }

        private bool ResetWords()
        {
            MessageBox("Are you sure you want to reset learned words?",
                       "Reset",
                       MessageBoxButtonType.YesNo,
                       onSave: result =>
            {
                DocsWhere("Words", "{'Words':[{'IsLearned':true,'IsArchived':false}]}")
                    .Update("{'Words':[{'IsLearned':false}]}");
            });

            return true;
        }

        private bool ArchiveWords()
        {
            MessageBox("Are you sure you want to archive learned words?",
                       "Reset",
                       MessageBoxButtonType.YesNo,
                       onSave: result =>
            {
                    DocsWhere("Words", "{'Words':[{'IsLearned':true}]}")
                        .Update("{'Words':[{'IsArchived':true}]}");
            });

            return true;
        }

        private bool OpenPrompt()
        {
            var topics = FirstDocOf("Articles")
                            .GetWhere("{'Articles':[{'IsLearned':false}]}")
                            .Values("{'Articles':[{'Name':$}]}");

            if (topics.Any())
            {
                var idx = new Random().Next() % topics.Count;

                var topic = topics[idx];

                FirstDocOf("Articles")
                    .GetWhere("{'Articles':[{'Name':@Name}]}", topic)
                    .Update("{'Articles':[{'IsLearned':true}]}");

                new
                {
                    Prompt = $"Now you are teacher of english. We are talking about low-code platforms and how to sale products. We have new topic to discuss: {topic}. Please repeat our topic then ask questions one by one according this topic."
                }
                .ToCollection("Prompt")
                .SetUIDimension("{'Prompt':{'ControlType':'RichTextBox'},'ReadOnly':true}")
                .OpenForm();
            }
            else
            {
                MessageBox("You have no topics to learn");
            }

            return true;
        }

        public override bool OnEventDimension(EventInfo info) =>
            info.AttrPath.LastPath switch
            {
                "Learn"     => DocsWhere("Words", "{'Words':[{'IsLearned':false}]}").OpenForm("{'Words':[{'Word':$,'Phrase':$,'Trans':$,'IsLearned':$,'LearnText':$}]}"),
                "NewWord"   => CreateNewDocForArray("NewWord", "Words", "{'Words':[$]}").OpenForm(),
                "Edit"      => FirstDocOf("Words").ExtendUIDimension("{'Layout':'','IsRawPage':false,'Style':'Hide:IsLearned,IsArchived,LearnText'}").ResetDimension(DimensionType.Pagination).OpenForm(),
                "LearnText" => LearnText(info),
                "Reset"     => ResetWords(),
                "MyLife"    => FirstDocOf("MyLife").OpenForm(),
                "Articles"  => FirstDocOf("Articles").OpenForm(),
                "Archive"   => ArchiveWords(),
                "Prompt"    => OpenPrompt(),
                _ => base.OnEventDimension(info)
            };

        public override object OnComputedDimension(ComputedInfo info) =>
            info.Variable switch
            {
                "LearnedWordsVariable" => DocsWhere("Words", "{'Words':[{'IsLearned':true}]}").Count("{'Words':[{'Word':$}]}"),
                "TotalWordsVariable"   => FirstDocOf("Words").Count("{'Words':[{'Word':$}]}"),
                _ => base.OnComputedDimension(info)
            };

        public override bool OnMovePageDimension(MoveInfo info)
        {
            if (info.Collection.Name == "Words")
            {
                SaveForm();
            }

            return true;
        }
    }
}
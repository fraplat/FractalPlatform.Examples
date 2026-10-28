using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Common.Enums;
using FractalPlatform.Database.Engine.Info;
using System;

namespace FractalPlatform.Examples.Applications.ManagePasswords
{
    public class ManagePasswordsApplication : BaseApplication
    {
        public override void OnStart() =>
            this.InputBox("EncryptPassword",
                          onSave: result =>
                          {
                              var password = result.FindFirstValue("EncryptPassword");

                              if (password?.Length >= 6)
                              {
                                  Client.SetDefaultCollection("Passwords")
                                        .SetDefaultDimension(DimensionType.Encryption)
                                        .GetBaseDoc()
                                        .Update("{'EncryptPassword':@Password}", password);

                                  FirstDocOf("Passwords")
                                        .OpenForm();
                              }
                              else
                              {
                                  MessageBox("Password should be more than 6 symbols.",
                                             "Wrong password length",
                                             MessageBoxButtonType.Cancel,
                                             result => OnStart());
                              }
                          });

        private bool GetPassword(MenuInfo info)
        {
            var password = DocsWhere("Passwords", info.AttrPath)
                             .Value("{'Passwords':[{'Password':$}]}");

            return true;
        }

        public override bool OnMenuDimension(MenuInfo info) =>
            info.Action switch
            {
                "CopyPassword" => GetPassword(info),
                "TypePassword" => GetPassword(info),
                _ => throw new NotImplementedException()
            };
    }
}

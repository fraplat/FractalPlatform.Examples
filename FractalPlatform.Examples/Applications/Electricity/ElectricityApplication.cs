using System.Net.NetworkInformation;
using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Examples.Applications.Electricity
{
    public class ElectricityApplication : BaseApplication
    {
        private bool PingHost(string host)
        {
            try
            {
                var ping = new Ping();

                var pingreply = ping.Send(host, 1000);

                return pingreply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public override bool OnTimerDimension(TimerInfo info)
        {
            var locations = DocsOf("Locations").Select<Location>();

            foreach (var location in locations)
            {
                if (location.HasElectricity != PingHost(location.IPAddress) && location.TelegramUserID > 0)
                {
                    location.HasElectricity = !location.HasElectricity;

                    DocsWhere("Locations", "{'Address':@Address}", location.Address)
                    .Update(@"{'HasElectricity':@HasElectricity,
                              'TextMessages':[Add,{'Provider':'Telegram',
                                                   'Receiver':@Receiver,
                                                   'Message':@Message,
                                                   'IsSent':false}]}",
                             location.HasElectricity,
                             location.TelegramUserID,
                             location.HasElectricity ? $"{location.Address} HAS electricity" : $"{location.Address} HAS NO electricity");
                }

                DocsWhere("Locations", "{'Address':@Address}", location.Address)
                .Update("{'LastPingTime':@Now}");
            }

            return true;
        }

        private bool NewLocation() =>
            CreateNewDocFor("NewLocation", "Locations").OpenForm(onSave: result =>
            {
                var gps = result.Collection
                                .Values("{'Lat':$,'Lng':$}");

                FirstDocOf("Dashboard").Update("{'Map':{'Points':[Add,{'Lat':@Lat,'Lng':@Lng}]}}", gps[0], gps[1]);

                result.NeedReloadData = true;
            });

        public override bool OnEventDimension(EventInfo info) =>
            info.Action switch
            {
                "NewLocation" => NewLocation(),
                "Locations"   => DocsOf("Locations").OpenForm(),
                _ => base.OnEventDimension(info)
            };

        public override void OnStart() => FirstDocOf("Dashboard").OpenForm();
    }
}
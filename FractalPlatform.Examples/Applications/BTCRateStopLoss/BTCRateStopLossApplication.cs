using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;

namespace FractalPlatform.Examples.Applications.BTCRateStopLoss
{
    public class BTCRateStopLossApplication : BaseApplication
    {
        public class ConfigInfo
        {
            public decimal MinValue { get; set; }
            public decimal MaxValue { get; set; }
            public string Receiver { get; set; }
            public bool IsSendMessages { get; set; }
        }

        public decimal GetBTCRate() =>
            decimal.Parse(REST.Get("https://api.coindesk.com/v1/bpi/currentprice.json")
                                     .ToCollection()
                                     .Value("{'bpi':{'USD':{'rate':$}}}"));
        
        public override bool OnTimerDimension(TimerInfo info)
        {
            var rate = GetBTCRate();

            var config = FirstDocOf("Config")
                               .SelectOne<ConfigInfo>();

            if ((rate < config.MinValue || rate > config.MaxValue) &&
                config.IsSendMessages)
            {
                FirstDocOf("Config")
                      .Update(@"{'IsSendMessages':false,
                                 'TextMessages':[Add,
                                            {'Provider':'Telegram',
                                             'Receiver':@Receiver,
                                             'Message':@Message,
                                             'IsSent':false}]}",
                        config.Receiver, //receiver
                        $"Current {rate} BTC rate.");
            }

            return base.OnTimerDimension(info);
        }

        private void Rate()
        {
            new
            {
                USD = GetBTCRate()
            }
            .ToCollection()
            .SetUIDimension("{'Style':'Save:Config;Cancel:Refresh'}")
            .OpenForm(onSave: result => FirstDocOf("Config").OpenForm(result => Rate()),
                      onCancel: result => Rate());
        }

        public override void OnStart() => Rate();
    }
}

using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.MessageQueue;
using Senparc.CO2NET.Trace;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;

namespace Senparc.Weixin.AI.MPSample
{
    /// <summary>
    /// 全时响应
    /// </summary>
    public class CustomFullTimeMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        const string DEFAULT_MESSAGE = @"欢迎使用 Senparc.Weixin SDK！

===开源地址===
Senparc.AI模块：https://github.com/Senparc/Senparc.AI
微信SDK：https://github.com/JeffreySu/WeiXinMPSDK";

        private string _appId => Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;

        /// <summary>
        /// 为中间件提供生成当前类的委托
        /// </summary>
        public static Func<Stream, PostModel, int, IServiceProvider, CustomMessageHandler> GenerateMessageHandler = (stream, postModel, maxRecordCount, serviceProvider)
                         => new CustomMessageHandler(stream, postModel, maxRecordCount, false /* 是否只允许处理加密消息，以提高安全性 */, serviceProvider: serviceProvider);

        public CustomFullTimeMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            //使用消息队列处理
            var smq = new SenparcMessageQueue();
            var smqKey = $"Chat-{OpenId}-{SystemTime.NowTicks}";
            smq.Add(smqKey, async () =>
            {
                try
                {
                    var handler = new SemanticAiHandler();
                    var factory = new ReponseMessageFactory(ServiceProvider);
                    var responseMessage = await factory.GetResponseMessageAsync(_appId, this, requestMessage, handler, false);
                    await factory.SendCustomMessageAsync(responseMessage, ApiEnlightener, _appId, OpenId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("Chatting Exception", $"ex:{ex.Message},stack:{ex.StackTrace}");
                }

            });

            return base.CreateResponseMessage<ResponseMessageNoResponse>();
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = DEFAULT_MESSAGE;
            return responseMessage;
        }
    }
}

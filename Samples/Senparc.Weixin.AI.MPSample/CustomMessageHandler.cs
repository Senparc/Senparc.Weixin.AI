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
    public class CustomMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        const string DEFAULT_MESSAGE = @"欢迎使用 Senparc.Weixin SDK！
===命令===
请输入命令进行互动：
s - 清空上下文，开始聊天
q - 退出聊天

===开源地址===
Senparc.AI模块：https://github.com/Senparc/Senparc.AI
微信SDK：https://github.com/JeffreySu/WeiXinMPSDK";

        private readonly IBaseObjectCacheStrategy _cache;
        private string GetCacheKey(string openId) => $"SenparcAI-Chat-{openId}";
        private string _appId => Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;

        /// <summary>
        /// 为中间件提供生成当前类的委托
        /// </summary>
        public static Func<Stream, PostModel, int, IServiceProvider, CustomMessageHandler> GenerateMessageHandler = (stream, postModel, maxRecordCount, serviceProvider)
                         => new CustomMessageHandler(stream, postModel, maxRecordCount, false /* 是否只允许处理加密消息，以提高安全性 */, serviceProvider: serviceProvider);

        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
            _cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            var messageContext = await base.GetCurrentMessageContext();
            //正式对话
            var cacheKey = GetCacheKey(OpenId);
            ChatMessages messages = await _cache.GetAsync<ChatMessages>(cacheKey);
            messages ??= new ChatMessages(OpenId);

            var requestHandler = await requestMessage.StartHandler()
                .Keyword("s", () =>
                {
                    var result = Task.Factory.StartNew(async () =>
                    {
                        messageContext.StorageData = "Chatting";
                        GlobalMessageContext.UpdateMessageContext(messageContext);//储存到缓存

                        //清除上下文
                        messages.CleanMessage();
                        await _cache.SetAsync(cacheKey, messages, TimeSpan.FromHours(1));
                        responseMessage.Content = "Chat 准备就绪，请开始对话！";
                        return responseMessage;
                    });
                    return result.Unwrap().GetAwaiter().GetResult() as IResponseMessageBase;
                })
                .Keyword("q", () =>
                {
                    messageContext.StorageData = null;
                    GlobalMessageContext.UpdateMessageContext(messageContext);//储存到缓存
                    responseMessage.Content = "ChatGPT 对话状态已退出，后续聊天不会消耗额度。";
                    return responseMessage;
                })
                .Default(async () =>
                {
                    if (messageContext.StorageData as string == "Chatting")
                    {
                        SenparcTrace.SendCustomLog("Chatting", $"收到消息：{requestMessage.Content}");

                        //使用消息队列处理
                        var smq = new SenparcMessageQueue();
                        var smqKey = $"ChatGPT-{OpenId}-{SystemTime.NowTicks}";
                        smq.Add(smqKey, async () =>
                        {
                            try
                            {
                                var handler = new SemanticAiHandler();
                                var factory = new ReponseMessageFactory(ServiceProvider);
                                var responseMessage = await factory.GetResponseMessageAsync(_appId, this, requestMessage, handler);
                                await factory.SendCustomMessageAsync(responseMessage, ApiEnlightener, _appId, OpenId);
                            }
                            catch (Exception ex)
                            {
                                SenparcTrace.SendCustomLog("Chatting Exception", $"ex:{ex.Message},stack:{ex.StackTrace}");
                            }

                        });

                        return base.CreateResponseMessage<ResponseMessageNoResponse>();
                    }
                    else
                    {
                        responseMessage.Content = DEFAULT_MESSAGE;
                        return responseMessage;
                    }
                });
            return requestHandler.GetResponseMessage() as IResponseMessageBase;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = DEFAULT_MESSAGE;
            return responseMessage;
        }
    }
}

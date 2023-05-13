using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;
using Senparc.AI;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI.MPSample
{
    /// <summary>
    /// 适时响应
    /// </summary>
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

                        //TODO：清空 Senparc.AI 层面的对话
                        ReponseMessageFactory.iWantToRunCollection.TryRemove(OpenId, out _);
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
                .Regex(@"plan (?<plan>[\.\s\S\w\W]+)", () =>
                {
                    RunPlan(requestMessage);
                    return base.CreateResponseMessage<ResponseMessageNoResponse>();
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
                                var isChat = true;
                                var responseMessage = await factory.GetResponseMessageAsync(_appId, this, requestMessage, handler, isChat);
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

        #region 任务处理

        private void RunPlan(RequestMessageText requestMessage)
        {
            //使用消息队列处理
            var smq = new SenparcMessageQueue();
            var smqKey = $"Plan-{OpenId}-{SystemTime.NowTicks}";
            smq.Add(smqKey, async () =>
            {

                var planText = Regex.Match(requestMessage.Content, @"plan (?<plan>[\.\s\S\w\W]+)").Groups["plan"].Value;

                await base.ApiEnlightener.SendText(this._appId, OpenId, "你正准备发送一条计划，正在生成：" + planText);

                //var _semanticAiHandler = base.ServiceProvider.GetService<SemanticAiHandler>();
                var _semanticAiHandler = new SemanticAiHandler();

                var iWantToRun = _semanticAiHandler
                       .IWantTo()
                       .ConfigModel(ConfigModel.TextCompletion, OpenId, "text-davinci-003")
                       .BuildKernel();

                var planner = iWantToRun.ImportSkill(new PlannerSkill(iWantToRun.Kernel)).skillList;

                var dir = System.IO.Directory.GetCurrentDirectory();

                var skillsDirectory = Path.Combine(dir, "skills");

                iWantToRun.ImportSkillFromDirectory(skillsDirectory, "SummarizeSkill");
                iWantToRun.ImportSkillFromDirectory(skillsDirectory, "WriterSkill");

                var promptText = $@"Generate 5 steps maximum:

{planText}
";

                var request = iWantToRun.CreateRequest(planText, planner["CreatePlan"]);

                var originalPlan = await iWantToRun.RunAsync(request);

                //await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync()
                var appId =
                await base.ApiEnlightener.SendText(this._appId, OpenId, "计划生成完毕，正在执行");

                var planResult = originalPlan.Result.Variables.ToPlan().PlanString;

                var executionResults = originalPlan.Result;

                int step = 1;
                int maxSteps = 10;
                while (!executionResults.Variables.ToPlan().IsComplete && step < maxSteps)
                {
                    var stepRequest = iWantToRun.CreateRequest(executionResults.Variables, false, planner["ExecutePlan"]);
                    var results = (await iWantToRun.RunAsync(stepRequest)).Result;
                    if (results.Variables.ToPlan().IsSuccessful)
                    {
                        if (results.Variables.ToPlan().IsComplete)
                        {
                            await base.ApiEnlightener.SendText(_appId, OpenId, "结果已生成：");

                            await base.ApiEnlightener.SendText(_appId, OpenId, results.Variables.ToPlan().Result);

                            break;
                        }
                    }
                    else
                    {
                        SenparcTrace.SendCustomLog("OpenAI Plan", results.LastException?.Message);
                        Console.WriteLine("Error Message:" + results.LastException?.Message);
                        Console.WriteLine(results.Variables.ToPlan().Result);
                        break;
                    }

                    executionResults = results;
                    step++;
                    Console.WriteLine("");
                }
            });
        }

        #endregion
    }
}

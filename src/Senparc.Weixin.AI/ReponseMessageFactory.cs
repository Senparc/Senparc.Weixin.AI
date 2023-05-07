using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Exceptions;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.MessageQueue;
using Senparc.NeuChar;
using Senparc.NeuChar.ApiHandlers;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin.AI.Entities;
using Senparc.Weixin.AI.WeixinSkills;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using Senparc.Weixin.MP.Entities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI
{
    public class ReponseMessageFactory
    {
        IServiceProvider ServiceProvider { get; set; }

        public ReponseMessageFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public async Task<string> GetResponseMessageByPlanAsync(IAiHandler aiHandler, string openId, string text)
        {
            //判断 OpenAI 是否已配置
            if (!Senparc.AI.Config.SenparcAiSetting.IsOpenAiKeysSetted)
            {
                //如果没有配置 OpenAI，则始终使用文字消息
                return nameof(ResponseMessageText);
            }

            //已经配置了 OpenAI，根据用户消息的提示，进行判断

            var skAiHandler = aiHandler as Senparc.AI.Kernel.SemanticAiHandler;
            var iWantToRun = skAiHandler
                             .IWantTo()
                             .ConfigModel(ConfigModel.TextCompletion, openId, "text-davinci-003")
                             .BuildKernel();

            var planner = iWantToRun.ImportSkill(new PlannerSkill(iWantToRun.Kernel)).skillList;
            var dir = System.IO.Directory.GetCurrentDirectory();
            await Console.Out.WriteLineAsync("dir:" + dir);


            var skillsDirectory = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/skills");/* Path.Combine(dir, "..", "..", "..", "skills");*/
            await Console.Out.WriteLineAsync("skillsDirectory:" + skillsDirectory);
            //var skillList = iWantToRun.ImportSkillFromDirectory(skillsDirectory, "ResponseChooseSkill").skillList;
            iWantToRun.ImportSkill(new SenparcWeixinSkills(iWantToRun.Kernel), "BuildResponseMessage");

            //var ask = "I want to know which program language is the best one?";

            var request = iWantToRun.CreateRequest(text, true, planner["CreatePlan"]);
            var originalPlan = await iWantToRun.RunAsync(request);

            var plannResult = originalPlan.Result.Variables.ToPlan().PlanString;
            await Console.Out.WriteLineAsync("Original plan:");
            await Console.Out.WriteLineAsync(plannResult);

            var executionResults = originalPlan.Result;

            int step = 1;
            int maxSteps = 10;
            string messageType = "UnKnow";
            while (!executionResults.Variables.ToPlan().IsComplete && step < maxSteps)
            {
                var stepRequest = iWantToRun.CreateRequest(executionResults.Variables, false, planner["ExecutePlan"]);
                var results = (await iWantToRun.RunAsync(stepRequest)).Result;
                if (results.Variables.ToPlan().IsSuccessful)
                {
                    Console.WriteLine($"Step {step} - Execution results:\n");
                    Console.WriteLine(results.Variables.ToPlan().PlanString);

                    if (results.Variables.ToPlan().IsComplete)
                    {
                        messageType = results.Variables.ToPlan().Result;
                        Console.WriteLine($"Step {step} - COMPLETE!");
                        Console.WriteLine(results.Variables.ToPlan().Result);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"Step {step} - Execution failed:");
                    Console.WriteLine("Error Message:" + results.LastException?.Message);
                    Console.WriteLine(results.Variables.ToPlan().Result);
                    break;
                }

                executionResults = results;
                step++;
                Console.WriteLine("");
            }

            await Console.Out.WriteLineAsync("== plan execute finish ==");

            return messageType;
        }

        public static ConcurrentDictionary<string, IWantToRun> iWantToRunCollection = new ConcurrentDictionary<string, IWantToRun>();

        public async Task<ResponseMessageResult> GetResponseMessagResultAsync(IAiHandler aiHandler, string openId, string text, bool isChat)
        {
            var skAiHandler = aiHandler as Senparc.AI.Kernel.SemanticAiHandler;

            IWantToRun iWantToRun = null;

            if (iWantToRunCollection.ContainsKey(openId))
            {

                iWantToRun = iWantToRunCollection[openId];
            }
            else
            {
                if (isChat)
                {
                    var parameter = new PromptConfigParameter()
                    {
                        MaxTokens = 2000,
                        Temperature = 0.7,
                        TopP = 0.5,
                    };
                    var chatConfig = skAiHandler.ChatConfig(parameter, userId: "User-" + openId);
                    iWantToRun = chatConfig.iWantToRun;
                }
                else
                {
                    iWantToRun = skAiHandler
                             .IWantTo()
                             .ConfigModel(ConfigModel.TextCompletion, openId, "text-davinci-003")
                             .BuildKernel();
                }

            }

            var dir = System.IO.Directory.GetCurrentDirectory();
            await Console.Out.WriteLineAsync("dir:" + dir);

            var skillsDirectory = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/skills");/* Path.Combine(dir, "..", "..", "..", "skills");*/
            await Console.Out.WriteLineAsync("skillsDirectory:" + skillsDirectory);
            var skillList = iWantToRun.ImportSkillFromDirectory(skillsDirectory, "ResponseChooseSkill").skillList;
            //iWantToRun.ImportSkill(new SenparcWeixinSkills(iWantToRun.Kernel), "BuildResponseMessage");

            //var ask = "I want to know which program language is the best one?";

            var request = iWantToRun.CreateRequest(text, true, skillList.Values.ToArray()/*planner["CreatePlan"]*/);

            string history = null;
            if (isChat)
            {
                //历史记录
                //初始化对话历史（可选）
                if (!request.GetStoredContext("history", out history))
                {
                    request.SetStoredContext("history", "");
                }

                //本次记录
                request.SetStoredContext("human_input", text);
                request.RequestContent = null;
            }

            SenaprcContentAiResult result = await iWantToRun.RunAsync(request);

            if (isChat)
            {
                //记录对话历史（可选）
                request.SetStoredContext("history", history + $"\nHuman: {text}\nBot: {request.RequestContent}");
            }


            ResponseMessageResult messageType = null;
            if (result.Output.Length > 0)
            {
                messageType = result.Output.GetObject<ResponseMessageResult>();
            }
            else
            {
                messageType = new ResponseMessageResult() { MessageType = ResponseMsgType.Unknown };
            }

            return messageType;
        }

        private async Task<SenparcAiResult> WeixinChat(IWantToRun iWantToRun, string prompt)
        {
            var request = iWantToRun.CreateRequest(true);

            //历史记录
            //初始化对话历史（可选）
            if (!request.GetStoredContext("history", out var history))
            {
                request.SetStoredContext("history", "");
            }

            //本次记录
            request.SetStoredContext("human_input", prompt);

            var newRequest = request with { RequestContent = null };

            //运行
            var aiResult = await iWantToRun.RunAsync(newRequest);

            //记录对话历史（可选）
            request.SetStoredContext("history", history + $"\nHuman: {prompt}\nBot: {request.RequestContent}");

            return aiResult;
        }

        /// <summary>
        /// 获取相应信息
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="messageHandler"></param>
        /// <param name="requestMessage"></param>
        /// <param name="aiHandler"></param>
        /// <returns></returns>
        public async Task<IResponseMessageBase> GetResponseMessageAsync(string appId, IMessageHandlerBase messageHandler, IRequestMessageText requestMessage, IAiHandler aiHandler, bool isChat)
        {
            var skAiHandler = aiHandler as SemanticAiHandler;
            var openId = messageHandler.WeixinOpenId;
            ResponseMessageResult messageTypeResult = await GetResponseMessagResultAsync(aiHandler, openId, requestMessage.Content, isChat);

            IResponseMessageBase responseMessage = ResponseMessageBase.CreateFromRequestMessage(requestMessage, messageTypeResult.MessageType, messageHandler.MessageEntityEnlightener);

            switch (messageTypeResult.MessageType)
            {
                case ResponseMsgType.Text:
                    {
                        var response = (IResponseMessageText)responseMessage;
                        response.Content = messageTypeResult.Result;
                        responseMessage = response;
                    }
                    break;
                case ResponseMsgType.Image:
                    {
                        var response = (IResponseMessageImage)responseMessage;
                        //调用 DallE 接口，并上传图片

                        //使用队列处理
                        var smq = new SenparcMessageQueue();
                        var smqKey = $"SenparcAI-{openId}-{SystemTime.NowTicks}";
                        smq.Add(smqKey, async () =>
                        {
                            _ = CustomApi.SendTextAsync(appId, openId, $"已收到请求，请等待约 1 分钟");

                            _ = CustomApi.SendTextAsync(appId, openId, "图片Prompt：" + messageTypeResult.Result);

                            //var iWantToRun = skAiHandler
                            //                    .IWantTo()
                            //                    .ConfigModel(ConfigModel.ImageGeneration, openId, "dalle")
                            //                    .BuildKernel();

                            //var dallE = iWantToRun.Kernel.GetService<IImageGeneration>();
                            //var imageUrl = await dallE.GenerateImageAsync(messageTypeResult.Result, 256, 256);
                            _ = CustomApi.SendTextAsync(appId, openId, $"正在准备图片，请等待...");

                            try
                            {
                                #region DallE 需要直接使用 OpenAI 接口，需要在中国外使用

                                //var ms = new MemoryStream();
                                //var downloadFile = Senparc.CO2NET.HttpUtility.Get.DownloadAsync(ServiceProvider, imageUrl, ms);
                                //ms.Seek(0, SeekOrigin.Begin);


                                ////缓存在本地
                                //var fileDir = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath($"~/App_Data/OpenAiImageTemp");
                                ////确认文件夹存在
                                //Senparc.CO2NET.Helpers.FileHelper.TryCreateDirectory(fileDir);

                                //var file = Path.Combine(fileDir, $"{SystemTime.Now.ToString("yyyyMMdd-HH-mm-ss")}_{openId}_{SystemTime.NowTicks}.jpg");

                                //using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                                //{
                                //    await ms.CopyToAsync(fs);
                                //    await fs.FlushAsync();
                                //}

                                ////创建一个副本，防止被占用
                                //var newFile = file + ".jpg";
                                //File.Copy(file, newFile);
                                #endregion

                                var newFile = CO2NET.Utilities.ServerUtility.DllMapPath("~/test.png")
                                                .Replace(@"\\Mac\Develop & Data\", @"Y:\");

                                //await CustomApi.SendTextAsync(appId, openId, $"new file:" + newFile);

                                //await CustomApi.SendTextAsync(appId, openId, $"new file exist:" + File.Exists(newFile));

                                //上传到微信素材库
                                var uploadResult = await MediaApi.UploadTemporaryMediaAsync(appId, Weixin.MP.UploadMediaFileType.image, newFile);

                                //await CustomApi.SendTextAsync(appId, openId, $"result:"+uploadResult.ToJson(true));

                                await CustomApi.SendImageAsync(appId, openId, uploadResult.media_id);

                                //删除临时文件
                                File.Delete(newFile);
                            }
                            catch (Exception ex)
                            {
                                new SenparcAiException(ex.Message, ex);
                                await CustomApi.SendTextAsync(appId, openId, $"图片生成失败！");
                            }

                            //预计会超过时间，直接返回文本信息

                            responseMessage = ResponseMessageBase.CreateFromRequestMessage(requestMessage, ResponseMsgType.NoResponse, messageHandler.MessageEntityEnlightener);
                        });
                    }
                    break;

                //case ResponseMsgType.Other:
                //    break;
                //case ResponseMsgType.Unknown:
                //    break;

                //case ResponseMsgType.News:
                //    break;
                //case ResponseMsgType.Music:
                //    break;
                //case ResponseMsgType.Voice:
                //    break;
                //case ResponseMsgType.Video:
                //    break;
                //case ResponseMsgType.Transfer_Customer_Service:
                //    break;
                //case ResponseMsgType.MpNews:
                //    break;
                //case ResponseMsgType.TaskCard:
                //    break;
                //case ResponseMsgType.MultipleNews:
                //    break;
                //case ResponseMsgType.LocationMessage:
                //    break;
                //case ResponseMsgType.NoResponse:
                //    break;
                //case ResponseMsgType.SuccessResponse:
                //    break;
                //case ResponseMsgType.UseApi:
                //    break;
                default:
                    goto case ResponseMsgType.Text;
            }
            return responseMessage;
        }

        /// <summary>
        /// 根据 ResposeMessage 类型自动发送客服消息
        /// </summary>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        public async Task SendCustomMessageAsync(IResponseMessageBase responseMessage, ApiEnlightener apiEnlightener, string appId, string openId)
        {
            //使用客服消息
            if (responseMessage is ResponseMessageText textResponse)
            {
                await apiEnlightener.SendText(appId, openId, textResponse.Content);

                //等效于：
                //await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_appId, OpenId, textResponse.Content);
            }
            else if (responseMessage is ResponseMessageImage imageResponse)
            {
                await apiEnlightener.SendImage(appId, openId, imageResponse.Image.MediaId);
            }
        }
    }
}

using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel.Handlers;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.AI.WeixinSkills;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI
{
    public class ReponseMessageFactory
    {
        public async Task<string> GetResponseMessageByPlanAsync(IAiHandler aiHandler, string openId, string text)
        {
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

            var request = iWantToRun.CreateRequest(text, true, false, planner["CreatePlan"]);
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
                var stepRequest = iWantToRun.CreateRequest(executionResults.Variables, false, false, planner["ExecutePlan"]);
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


        public async Task<string> GetResponseMessageAsync(IAiHandler aiHandler, string openId, string text)
        {
            var skAiHandler = aiHandler as Senparc.AI.Kernel.SemanticAiHandler;
            var iWantToRun = skAiHandler
                             .IWantTo()
                             .ConfigModel(ConfigModel.TextCompletion, openId, "text-davinci-003")
                             .BuildKernel();

            var dir = System.IO.Directory.GetCurrentDirectory();
            await Console.Out.WriteLineAsync("dir:" + dir);


            var skillsDirectory = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/skills");/* Path.Combine(dir, "..", "..", "..", "skills");*/
            await Console.Out.WriteLineAsync("skillsDirectory:" + skillsDirectory);
            var skillList = iWantToRun.ImportSkillFromDirectory(skillsDirectory, "ResponseChooseSkill").skillList;
            //iWantToRun.ImportSkill(new SenparcWeixinSkills(iWantToRun.Kernel), "BuildResponseMessage");

            //var ask = "I want to know which program language is the best one?";

            var request = iWantToRun.CreateRequest(text, true, false, skillList.Values.ToArray()/*planner["CreatePlan"]*/);
            var result = await iWantToRun.RunAsync(request);

            string messageType = "UnKnow";
            if (result.Output.Length>0)
            {
                messageType = result.Output;
            }

            return messageType;
        }
    }
}

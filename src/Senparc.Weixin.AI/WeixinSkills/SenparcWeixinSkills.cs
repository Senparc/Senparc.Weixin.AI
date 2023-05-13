using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions.Partitioning;
using Microsoft.SemanticKernel.SkillDefinition;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI.WeixinSkills
{
    public class SenparcWeixinSkills
    {
        /// <summary>
        /// The max tokens to process in a single semantic function call.
        /// </summary>
        private const int MaxTokens = 1024;

        private readonly ISKFunction _buildResponseMessageFunction;

        private const string BUILD_RESPONSE_MESSAGE_DEFINITION = @"Determine the type of message and result that should be returned based on the input information.

The context of the request may contains several history messages, only the last message can determine the type of the message, which always using the key of ""input"" in context.

There are two types of message:
- Text: just need return text, without image required. The Result parameter will be returned as a ChatBot's reply.
- Image: need to generate image. The Result parameter will give a better prompt to generate a image by general image generate engine.

Examples:

Input: What's the mean of animal?
Output: [{""MessageType"":""Text"", ""Result"":""The word \""animal\"" is a broad term that refers to living organisms that are not plants or fungi, and it encompasses a wide range of species with diverse characteristics. Therefore, it is not possible to calculate a mean for the term ""animal"" as it is not a numerical value or a quantitative measure.""}]

Input: Can you tell me what's in the image?
Output: [{""MessageType"":""Text"", ""Result"":""It's an image of a panda driving a car in the sky, with the car flying high and the panda looking happy and excited.""}]

Input: Give me a picture with sky and bird.
Output: [{""MessageType"":""Image"", ""Result"":""The sky is filled with colorful birds.""}]

Input: Give me an image shows the spring of China.
Output: [{""MessageType"":""Image"", ""Result"":""Generate a high-quality and visually stunning image that depicts the beauty of spring in China, with a focus on blooming flowers, lush green landscapes, and perhaps a cultural or historical element such as a traditional Chinese garden or temple.""}]

[done]


The original Output is JSON format, I want to get an XML format result, do the parse first. Just left JSON information without any other 'appendToResult' information.

Now tell me the follow message's output:
Input: {{$input}}
Output: ";

        public SenparcWeixinSkills(IKernel kernel)
        {
            _buildResponseMessageFunction = kernel.CreateSemanticFunction(
            BUILD_RESPONSE_MESSAGE_DEFINITION,
            skillName: nameof(SenparcWeixinSkills),
            description: "Generate Response Message Entity.",
            maxTokens: MaxTokens,
            temperature: 0.1,
            topP: 0.5);

        }

        /// <summary>
        /// Given a long conversation transcript, identify topics.
        /// </summary>
        /// <param name="input">A long conversation transcript.</param>
        /// <param name="context">The SKContext for function execution.</param>
        [SKFunction("Generate Response Message Entity")]
        [SKFunctionName("BuildResponseMessage")]
        [SKFunctionInput(Description = "Response message data")]
        public Task<SKContext> BuildResponseMessageAsync(string input, SKContext context)
        {
            List<string> lines = SemanticTextPartitioner.SplitPlainTextLines(input, MaxTokens);
            List<string> paragraphs = SemanticTextPartitioner.SplitPlainTextParagraphs(lines, MaxTokens);

            return this._buildResponseMessageFunction
                .AggregatePartitionedResultsAsync(paragraphs, context);
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Weixin.AI.MPSample
{
    /// <summary>
    /// Chat 消息
    /// </summary>
    public class ChatMessages
    {

        /// <summary>
        /// 用户 ID
        /// </summary>
        public string UserId { get; private set; }
        /// <summary>
        /// 消息列表
        /// </summary>
        public List<string> Messages { get; private set; }

        /// <summary>
        /// 获取缓存 Key
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string GetCacheKey(string userId)
        {
            return $"ChatGPT-{userId}";
        }

        public ChatMessages(string userId, List<string> messages = null)
        {
            UserId = userId;
            Messages = messages ?? new List<string>();
        }

        /// <summary>
        /// 添加消息
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(string message)
        {
            Messages.Add(message);
        }

        /// <summary>
        /// 清空消息
        /// </summary>
        public void CleanMessage()
        {
            Messages = new List<string>();
        }
       
    }
}

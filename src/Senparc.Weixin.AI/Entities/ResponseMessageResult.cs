using Senparc.NeuChar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Weixin.AI.Entities
{
    /// <summary>
    /// AI Skill 返回的结果（原格式为 Json）
    /// </summary>
    public class ResponseMessageResult
    {
        public ResponseMsgType MessageType { get; set; }
        public string Result { get; set; }
    }
}

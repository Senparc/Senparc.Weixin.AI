# Senparc.Weixin.AI
Senparc.Weixin SDK 的 AI 扩展，赋能微信应用基于 OpenAI API、ChatGPT 等 AIGC 能力。


## 使用说明

> 注意：Senparc.AI 基于 Senparc.CO2NET 等盛派全家桶的能力，可无缝对接到 Senparc.Weixin SDK 项目中。

### 第一步：配置

首先，按照 [Senparc.Weixin SDK](https://github.com/JeffreySu/WeiXinMPSDK) 的标准格式，在 appsettings.json 文件内设置常规的微信配置信息，以公众号为例：

默认项目 appsettings.json 内容如下：
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

在 JSON 中添加以下 CO2NET 及 Senaprc.Weixin SDK 配置：
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  //CO2NET 设置
  "SenparcSetting": {
    "IsDebug": true,
    "DefaultCacheNamespace": "DefaultCacheTest"
  },
  //Senparc.Weixin SDK 设置
  "SenparcWeixinSetting": {
    //微信全局
    "IsDebug": true,
    //公众号
    "Token": "YourToken",
    "EncodingAESKey": "YourEncodingAESKey",
    "WeixinAppId": "YourWeixinAppId",
    "WeixinAppSecret": "YourWeixinAppSecret"
  }
}
```

为使用 Senaprc.AI，在原有项目配置的基础上，继续追加一段 Senparc.AI 的配置：

```
  //Senparc.AI 设置
  "SenparcAiSetting": {
    "IsDebug": true,
    "AiPlatform": "AzureOpenAI",
    "AzureOpenAIKeys": {
      "ApiKey": "YourAzureApiKey", 
      "AzureEndpoint": "https://xxxx.openai.azure.com/",
      "AzureOpenAIApiVersion": "2022-12-01" 
    },
    "OpenAIKeys": {
      "ApiKey": "YourOpenAIKey", 
      "OrgaizationId": "YourOpenAIOrgId"
    }
  }
```

> 各项配置含义详见：https://github.com/Senparc/Senparc.AI

### 第二步：设置 MessageHandler

> 您可以在已经创建好的 MessageHandler 中继续集成 Senparc.AI（以及 Senparc.Weixin.AI），机器人默认使用文字方式接收和应答，但当内容被判断为需要输出图片时，使用 OpenAI 的 DallE 接口生成图片（注意：如果 `appsettings.json`文件中未配置 OpenAIKeys 信息，将始终使用文字方式返回）。<br>
> 下面以一个全新的文件为例，创建一个对话聊天机器人。

新创建的 CustomFullTimeMessageHandler.cs：
```
public class CustomFullTimeMessageHandler : MessageHandler<DefaultMpMessageContext>
{
    const string DEFAULT_MESSAGE = @"欢迎使用 Senparc.Weixin SDK！

===开源地址===
Senparc.AI模块：https://github.com/Senparc/Senparc.AI
微信SDK：https://github.com/JeffreySu/WeiXinMPSDK";


    public CustomFullTimeMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
    {
    }

    public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
    {
        var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
        responseMessage.Content = DEFAULT_MESSAGE;
        return responseMessage;
    }
}
```

从上述的默认消息 `DEFAULT_MESSAGE` 说明文字中，可以看到当前我们希望创建的机器人的对话方式：

由于我们需要在公众号上接收用户发来的文字信息，因此配置接收方法：
```
public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
{
    //TODO..

    return base.CreateResponseMessage<ResponseMessageNoResponse>();
}
```

在上述 `//TODO...` 位置加入一个队列对象，这么做的好处是可以拜托微信公众号必须在规定时间内回复消息的限制，独立的线程完成工作后，将使用客服消息进行回复（注意：上述返回类型 `ResponseMessageNoResponse` 可以避免出现不返回任何消息时，出现”公众号服务发生故障“的错误信息），代码如下：

```
//使用消息队列处理
var smq = new SenparcMessageQueue();
var smqKey = $"Chat-{OpenId}-{SystemTime.NowTicks}";
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
```

只需运行上述代码，即可实现和 ChatGPT 一致的的对话功能。

上述代码解析如下：

`var smq = new SenparcMessageQueue();` 用于定义一个 `SenparcMessageQueue` 队列操作对象。

`var smqKey = $"Chat-{OpenId}-{SystemTime.NowTicks}";` 定义类了这个队列的唯一编号（全局不能重复，否则会被覆盖）。

`smq.Add(smqKey, async () =>{...});` 用于创建一个队列请求。

`var handler = new SemanticAiHandler();` 用于创建一个 `SemanticAiHandler` 对象，负责处理 AI 业务。

`var factory = new ReponseMessageFactory(ServiceProvider);` 用于创建一个微信响应消息工厂。

`var responseMessage = await factory.GetResponseMessageAsync(...)` 使用微信响应消息工厂，自动根据用户发送的消息，得到响应信息，可能会是文本信息，也可能是图片信息，取决于用户发送消息的内容（注意：当 OpenAI 接口未配置时，始终为文字消息）。

`await factory.SendCustomMessageAsync(...)` 用于根据上一步获取到的 `responseMessage` 对象，自动发送客服消息。这里没有直接在上一步就自动发送的原因，是希望在过程中暴露 `responseMessage`，给开发者更多的自由，例如添加到审计日志，或进行其他操作等。

### 第三步：发布

以公众号（网站）常规方式发布即可，无需特殊处理。

## 进阶：自动管理对话状态

Senparc 全家桶提供了完整的缓存和上下文处理能力，在上述代码中，可以直接进行改造，以支持具备状态管理的对话机器人：只在特定的命令出现时，初始化并进入到对话状态，并在特定命令出现时，退出对话状态。这样做的好处有几个：

1. 可以节省 token 的消耗，只在需要的时候请求 AI 资源。
2. 方便随时开始新的对话，原有的方法会一直增加对话的轮数。
3. 让程序同时支持更多的应用场景，只在特定情况下进入到对话机器人状态。

下面让我们对上述的代码进行一下改造：


### 第一步：引入缓存

在 MessageHanndler 中创建一个 `IBaseObjectCacheStrategy` 变量，并在构造函数中初始化：

```
private readonly IBaseObjectCacheStrategy _cache;

public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
{
    _cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
}
```

在项目 `appsettings.json` 文件中，没有特别配置缓存信息，因此默认使用内存缓存。

> 注意：无论使用何种缓存，都不需要在业务代码中做棵树处理，只需要在 `appsettings.json` 文件中配置缓存服务的连接字符串并激活即可。这里不展开。

### 第二步：更新消息处理过程

我们设定一下规则：当用户输入字母 `s` 时，开启新的对话，用户输入 `q` 时结束对话。

修改 `OnTextRequestAsync()` 方法如下：

```
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
```

### 第三步：修改默认消息(可选)

```
const string DEFAULT_MESSAGE = @"欢迎使用 Senparc.Weixin SDK！
===命令===
请输入命令进行互动：
s - 清空上下文，开始聊天
q - 退出聊天

===开源地址===
Senparc.AI模块：https://github.com/Senparc/Senparc.AI
微信SDK：https://github.com/JeffreySu/WeiXinMPSDK";
```
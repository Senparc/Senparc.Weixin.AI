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

### 设置 MessageHandler

> 您可以在已经创建好的 MessageHandler 中继续集成 Senparc.AI（以及 Senparc.Weixin.AI），机器人默认使用文字方式接收和应答，但当内容被判断为需要输出图片时，使用 OpenAI 的 DallE 接口生成图片（注意：如果 `appsettings.json`文件中未配置 OpenAIKeys 信息，将始终使用文字方式返回）。<br>
> 下面以一个全新的文件为例，创建一个对话聊天机器人。



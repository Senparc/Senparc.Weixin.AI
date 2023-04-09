using Senparc.Weixin.AI.MPSample;
using Senparc.Weixin.AspNet;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.RegisterServices;

var builder = WebApplication.CreateBuilder(args);


//使用本地缓存必须添加
builder.Services.AddMemoryCache();

#region 添加微信配置（一行代码）

//Senparc.Weixin 注册（必须）
builder.Services.AddSenparcWeixinServices(builder.Configuration);

#endregion

var app = builder.Build();

#region 启用微信配置（一句代码）

//启用微信配置（必须）
var registerService = app.UseSenparcWeixin(app.Environment,
    null, null,
    register => { /* CO2NET 全局配置 */ },
    (register, weixinSetting) =>
    {
        //注册公众号信息（可以执行多次，注册多个公众号）
        register.RegisterMpAccount(weixinSetting, "我的公众号");
    });

#endregion

#region 使用 MessageHadler 中间件，用于取代创建独立的 Controller

//MessageHandler 中间件介绍：https://www.cnblogs.com/szw/p/Wechat-MessageHandler-Middleware.html
//使用公众号的 MessageHandler 中间件（不再需要创建 Controller）
app.UseMessageHandlerForMp("/WeixinAsync", CustomMessageHandler.GenerateMessageHandler, options =>
{
    options.AccountSettingFunc = context => Senparc.Weixin.Config.SenparcWeixinSetting;
});

#endregion


app.MapGet("/", () => "欢迎使用 Senparc.AI + Senparc.Weixin !");

app.Run();
namespace Senparc.AI.Kernel
{
    /// <summary>
    /// Senparc.AI.Kernel 配置
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 当前配置
        /// </summary>
        public static SenparcAiSettings SenparcAiSettings { get; set; }

        static Config()
        {
            //初始化 SenaprcAiSettings
            SenparcAiSettings = new SenparcAiSettings();
        }
    }
}

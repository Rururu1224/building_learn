using System;
using System.Collections.Generic;

namespace BuildingFireTest.Interfaces
{
    /// <summary>
    /// 系统消息数据结构
    /// </summary>
    public class MasterMessage
    {
        /// <summary>消息时间，格式 HH:mm:ss</summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>消息内容</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>是否为提示/警告类消息（黄色显示）</summary>
        public bool IsWarning { get; set; }
    }

    /// <summary>
    /// 温度数据结构（5通道）
    /// </summary>
    public class TemperatureData
    {
        /// <summary>炉温1（TF1）</summary>
        public double TempFurnace1 { get; set; }

        /// <summary>炉温2（TF2）</summary>
        public double TempFurnace2 { get; set; }

        /// <summary>表面温度（TS）</summary>
        public double TempSurface { get; set; }

        /// <summary>中心温度（TC）</summary>
        public double TempCenter { get; set; }

        /// <summary>校准温度（TCal）</summary>
        public double TempCalibration { get; set; }

        /// <summary>时间戳（秒）</summary>
        public int TimeSeconds { get; set; }
    }

    /// <summary>
    /// 试验状态枚举
    /// </summary>
    public enum TestState
    {
        Idle,
        Preparing,
        Ready,
        Recording,
        Complete
    }

    /// <summary>
    /// 数据广播事件参数 —— 由B层通过DataBroadcast事件推送
    /// </summary>
    public class DataBroadcastEventArgs : EventArgs
    {
        /// <summary>当前温度数据</summary>
        public TemperatureData Temperature { get; set; } = new();

        /// <summary>当前试验状态</summary>
        public TestState CurrentState { get; set; }

        /// <summary>状态文字描述</summary>
        public string StateText { get; set; } = string.Empty;

        /// <summary>已记录秒数（仅Recording状态有效）</summary>
        public int RecordingSeconds { get; set; }

        /// <summary>10分钟温漂值（°C/10min）</summary>
        public double TemperatureDrift { get; set; }

        /// <summary>当前样品编号</summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>当前试验标识</summary>
        public string TestId { get; set; } = string.Empty;

        /// <summary>本次推送的系统消息列表</summary>
        public List<MasterMessage> Messages { get; set; } = new();

        /// <summary>是否有未保存的完成试验</summary>
        public bool HasUnsavedComplete { get; set; }
    }
}
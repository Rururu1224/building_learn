using System;

namespace BuildingFireTest.Interfaces
{
    /// <summary>
    /// 核心业务层接口 —— 由人员B实现
    /// UI层通过此接口调用所有业务逻辑，不直接操作仿真/状态机
    /// </summary>
    public interface ICoreService
    {
        // ========== 事件 ==========

        /// <summary>数据广播事件（后台线程触发，UI需Invoke处理）</summary>
        event EventHandler<DataBroadcastEventArgs> DataBroadcast;

        // ========== 试验控制 ==========

        /// <summary>新建试验</summary>
        /// <param name="testInfo">试验信息（由新建试验弹窗收集）</param>
        /// <returns>是否创建成功</returns>
        bool CreateNewTest(TestCreationInfo testInfo);

        /// <summary>开始升温（Idle → Preparing）</summary>
        void StartHeating();

        /// <summary>停止升温（Preparing/Ready → Idle）</summary>
        void StopHeating();

        /// <summary>开始记录（Ready → Recording）</summary>
        void StartRecording();

        /// <summary>停止记录（Recording → Complete）</summary>
        void StopRecording();

        /// <summary>保存试验现象记录（Complete状态下调用）</summary>
        /// <param name="record">试验现象数据</param>
        /// <returns>是否保存成功</returns>
        bool SaveTestRecord(TestPhenomenonRecord record);

        // ========== 状态查询 ==========

        /// <summary>获取当前试验状态</summary>
        TestState GetCurrentState();

        /// <summary>获取当前状态文字描述</summary>
        string GetStateText();

        /// <summary>是否有未保存的完成试验</summary>
        bool HasUnsavedCompleteTest();

        /// <summary>是否有活动试验（已有试验但未完成保存）</summary>
        bool HasActiveTest();

        // ========== 校准相关 ==========

        /// <summary>获取当前校准温度</summary>
        double GetCalibrationTemperature();

        /// <summary>记录校准数据点</summary>
        void RecordCalibrationPoint(double standardTemp);

        // ========== 用户登录 ==========

        /// <summary>验证登录</summary>
        /// <param name="role">角色：admin / experimenter</param>
        /// <param name="password">密码</param>
        /// <returns>null=登录成功，否则返回错误消息</returns>
        string? Login(string role, string password);

        /// <summary>获取当前登录用户角色</summary>
        string GetCurrentUserRole();
    }

    /// <summary>
    /// 新建试验信息 —— 从UI弹窗收集后传给B层
    /// </summary>
    public class TestCreationInfo
    {
        // 环境信息
        public double EnvironmentTemp { get; set; }
        public double EnvironmentHumidity { get; set; }

        // 样品信息
        public string ProductId { get; set; } = string.Empty;
        public string TestId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public double Height { get; set; }      // mm
        public double Diameter { get; set; }     // mm

        // 试验参数
        public string Operator { get; set; } = string.Empty;
        public bool IsStandardDuration { get; set; } = true;  // true=60分钟，false=自定义
        public int CustomDurationMinutes { get; set; }        // 仅非标准模式有效

        // 初始质量
        public double PreWeight { get; set; }    // 克
    }

    /// <summary>
    /// 试验现象记录 —— 从UI弹窗收集后传给B层
    /// </summary>
    public class TestPhenomenonRecord
    {
        /// <summary>是否出现持续火焰</summary>
        public bool HasFlame { get; set; }

        /// <summary>火焰发生时刻（秒）</summary>
        public int FlameStartTime { get; set; }

        /// <summary>火焰持续时间（秒）</summary>
        public int FlameDuration { get; set; }

        /// <summary>试验后质量（克）</summary>
        public double PostWeight { get; set; }

        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
    }
}
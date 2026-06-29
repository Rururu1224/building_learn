using System;
using System.Collections.Generic;
using System.Data;

namespace BuildingFireTest.Interfaces
{
    /// <summary>
    /// 数据持久化层接口 —— 由人员C实现
    /// UI层通过此接口进行数据查询和导出，不直接操作数据库或文件
    /// </summary>
    public interface IDataService
    {
        // ========== 历史查询 ==========

        /// <summary>按条件查询历史试验记录</summary>
        /// <param name="startDate">开始日期（可为null）</param>
        /// <param name="endDate">结束日期（可为null）</param>
        /// <param name="productId">样品编号模糊搜索（可为null）</param>
        /// <param name="operatorName">操作员筛选（可为null）</param>
        /// <returns>查询结果DataTable</returns>
        DataTable QueryTestRecords(
            DateTime? startDate,
            DateTime? endDate,
            string? productId,
            string? operatorName);

        /// <summary>获取试验完整详情</summary>
        /// <param name="productId">样品编号</param>
        /// <param name="testId">试验标识</param>
        /// <returns>试验详情数据</returns>
        TestDetailInfo? GetTestDetail(string productId, string testId);

        // ========== 操作员列表 ==========

        /// <summary>获取所有操作员名称列表（用于查询下拉框）</summary>
        List<string> GetOperatorNames();

        // ========== 设备信息 ==========

        /// <summary>获取设备信息（自动填入新建试验弹窗）</summary>
        DeviceInfo GetDeviceInfo();

        // ========== 校准记录 ==========

        /// <summary>获取所有校准历史记录</summary>
        DataTable GetCalibrationRecords();

        // ========== 导出服务 ==========

        /// <summary>导出Excel报告</summary>
        /// <param name="productId">样品编号</param>
        /// <param name="testId">试验标识</param>
        /// <returns>导出文件路径</returns>
        string ExportExcel(string productId, string testId);

        /// <summary>导出PDF报告</summary>
        /// <param name="productId">样品编号</param>
        /// <param name="testId">试验标识</param>
        /// <returns>导出文件路径</returns>
        string ExportPdf(string productId, string testId);

        /// <summary>批量导出查询结果为Excel</summary>
        /// <param name="records">查询结果DataTable</param>
        /// <returns>导出文件路径</returns>
        string ExportQueryResults(DataTable records);
    }

    /// <summary>
    /// 试验详情
    /// </summary>
    public class TestDetailInfo
    {
        // 基本信息
        public string ProductId { get; set; } = string.Empty;
        public string TestId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Diameter { get; set; }

        // 试验参数
        public string Operator { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }
        public int TotalTestTime { get; set; }
        public double EnvironmentTemp { get; set; }
        public double EnvironmentHumidity { get; set; }

        // 质量与计算
        public double PreWeight { get; set; }
        public double PostWeight { get; set; }
        public double LostWeight { get; set; }
        public double LostWeightPercent { get; set; }

        // 温升
        public double TempRiseFurnace1 { get; set; }
        public double TempRiseFurnace2 { get; set; }
        public double TempRiseSurface { get; set; }
        public double TempRiseCenter { get; set; }
        public double DeltaTf { get; set; }

        // 火焰
        public bool HasFlame { get; set; }
        public int FlameStartTime { get; set; }
        public int FlameDuration { get; set; }

        // 判定
        public string Flag { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime CalibrationDate { get; set; }
        public double ConstPower { get; set; }
    }
}
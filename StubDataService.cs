using System;
using System.Collections.Generic;
using System.Data;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest
{
    /// <summary>
    /// IDataService 桩实现 —— 仅供UI独立开发/测试使用
    /// 联调时替换为人员C的 DbHelper 真实实现
    /// </summary>
    public class StubDataService : IDataService
    {
        public DataTable QueryTestRecords(
            DateTime? startDate, DateTime? endDate,
            string? productId, string? operatorName)
        {
            // 返回空DataTable（结构正确即可，UI测试用）
            var dt = new DataTable();
            dt.Columns.Add("productid", typeof(string));
            dt.Columns.Add("testid", typeof(string));
            dt.Columns.Add("productname", typeof(string));
            dt.Columns.Add("testdate", typeof(DateTime));
            dt.Columns.Add("operator", typeof(string));
            dt.Columns.Add("totaltesttime", typeof(int));
            dt.Columns.Add("preweight", typeof(double));
            dt.Columns.Add("postweight", typeof(double));
            dt.Columns.Add("lostweight_per", typeof(double));
            dt.Columns.Add("deltatf", typeof(double));
            dt.Columns.Add("flag", typeof(string));
            return dt;
        }

        public TestDetailInfo? GetTestDetail(string productId, string testId)
        {
            return new TestDetailInfo
            {
                ProductId = productId,
                TestId = testId,
                ProductName = "测试样品",
                Specification = "标准",
                Height = 50.0,
                Diameter = 45.0,
                Operator = "admin",
                TestDate = DateTime.Now,
                TotalTestTime = 3600,
                EnvironmentTemp = 25.0,
                EnvironmentHumidity = 50.0,
                PreWeight = 100.0,
                PostWeight = 98.5,
                LostWeight = 1.5,
                LostWeightPercent = 1.5,
                TempRiseFurnace1 = 725.0,
                TempRiseFurnace2 = 724.5,
                TempRiseSurface = 680.0,
                TempRiseCenter = 620.0,
                DeltaTf = 680.0,
                HasFlame = false,
                Flag = "10000000",
                Remark = ""
            };
        }

        public List<string> GetOperatorNames()
        {
            return new List<string> { "admin", "experimenter" };
        }

        public DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                DeviceId = "DEV-001",
                DeviceName = "不燃性试验炉",
                CalibrationDate = DateTime.Now.AddMonths(-6),
                ConstPower = 2048
            };
        }

        public DataTable GetCalibrationRecords()
        {
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("calibration_date", typeof(DateTime));
            dt.Columns.Add("standard_temp", typeof(double));
            dt.Columns.Add("measured_temp", typeof(double));
            dt.Columns.Add("operator", typeof(string));
            return dt;
        }

        public string ExportExcel(string productId, string testId)
        {
            return $"D:\\ISO11820\\Reports\\{testId}_报告.xlsx";
        }

        public string ExportPdf(string productId, string testId)
        {
            return $"D:\\ISO11820\\Reports\\{testId}_报告.pdf";
        }

        public string ExportQueryResults(DataTable records)
        {
            return $"D:\\ISO11820\\Reports\\查询结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }
    }
}
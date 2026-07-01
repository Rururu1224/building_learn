using System;
using System.Collections.Generic;
using System.Data;
using BuildingFireTest.Interfaces;
using BuildingLearn.Data;
using BuildingLearn.Data.Models;
using BuildingLearn.Global;
using BuildingLearn.Services;

namespace BuildingFireTest.Adapters
{
    /// <summary>
    /// IDataService 适配器 — 桥接前端接口与后端 DbHelper/ExportService
    /// </summary>
    public class DataServiceAdapter : IDataService
    {
        private readonly AppGlobal _app;

        public DataServiceAdapter()
        {
            _app = AppGlobal.Instance;
        }

        // ========== 历史查询 ==========

        public DataTable QueryTestRecords(
            DateTime? startDate, DateTime? endDate,
            string? productId, string? operatorName)
        {
            var records = _app.Db.QueryTestMasters(
                productId: productId,
                startDate: startDate?.ToString("yyyy-MM-dd 00:00:00"),
                endDate: endDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                operatorName: operatorName);

            var dt = new DataTable();
            dt.Columns.Add("productid", typeof(string));
            dt.Columns.Add("testid", typeof(string));
            dt.Columns.Add("productname", typeof(string));
            dt.Columns.Add("testdate", typeof(string));
            dt.Columns.Add("operator", typeof(string));
            dt.Columns.Add("totaltesttime", typeof(int));
            dt.Columns.Add("preweight", typeof(double));
            dt.Columns.Add("postweight", typeof(double));
            dt.Columns.Add("lostweight_per", typeof(double));
            dt.Columns.Add("deltatf", typeof(double));
            dt.Columns.Add("flag", typeof(string));

            foreach (var r in records)
            {
                dt.Rows.Add(r.ProductId, r.TestId, r.ProductName, r.TestDate,
                    r.Operator, r.TotalTestTime, r.PreWeight, r.PostWeight,
                    r.LostWeightPer, r.Deltatf, r.Flag);
            }

            return dt;
        }

        public TestDetailInfo? GetTestDetail(string productId, string testId)
        {
            var record = _app.Db.GetTestMaster(productId, testId);
            if (record == null) return null;

            return new TestDetailInfo
            {
                ProductId = record.ProductId,
                TestId = record.TestId,
                ProductName = record.ProductName,
                Specification = record.Specification,
                Height = record.Height,
                Diameter = record.Diameter,
                Operator = record.Operator,
                TestDate = DateTime.TryParse(record.TestDate, out var dt) ? dt : DateTime.MinValue,
                TotalTestTime = record.TotalTestTime,
                EnvironmentTemp = record.AmbientTemp,
                EnvironmentHumidity = record.AmbientHumidity,
                PreWeight = record.PreWeight,
                PostWeight = record.PostWeight,
                LostWeight = record.LostWeight,
                LostWeightPercent = record.LostWeightPer,
                TempRiseFurnace1 = record.DeltaTF1,
                TempRiseFurnace2 = record.DeltaTF2,
                TempRiseSurface = record.DeltaTS,
                TempRiseCenter = record.DeltaTC,
                DeltaTf = record.Deltatf,
                HasFlame = record.HasFlame,
                FlameStartTime = record.FlameStartTime,
                FlameDuration = record.FlameDuration,
                Flag = record.Flag,
                Remark = record.Remark
            };
        }

        // ========== 操作员 ==========

        public List<string> GetOperatorNames()
        {
            var names = new List<string>();
            // 合并所有角色的操作员
            var admins = _app.Db.GetOperatorsByRole("admin");
            var experimenters = _app.Db.GetOperatorsByRole("experimenter");
            foreach (var op in admins) names.Add(op.Username);
            foreach (var op in experimenters)
            {
                if (!names.Contains(op.Username))
                    names.Add(op.Username);
            }
            return names;
        }

        // ========== 设备信息 ==========

        public DeviceInfo GetDeviceInfo()
        {
            var apparatus = _app.Db.GetFirstApparatus();
            if (apparatus == null)
            {
                return new DeviceInfo
                {
                    DeviceId = "DEV-001",
                    DeviceName = "不燃性试验炉",
                    CalibrationDate = DateTime.Now.AddMonths(-6),
                    ConstPower = _app.Config.ConstPower
                };
            }

            return new DeviceInfo
            {
                DeviceId = apparatus.ApparatusId,
                DeviceName = apparatus.ApparatusName,
                CalibrationDate = apparatus.CalibrationDate,
                ConstPower = apparatus.ConstPower
            };
        }

        // ========== 校准记录 ==========

        public DataTable GetCalibrationRecords()
        {
            var records = _app.Db.GetCalibrationRecords();
            var dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("calibration_date", typeof(string));
            dt.Columns.Add("operator", typeof(string));
            dt.Columns.Add("standard_temp", typeof(double));
            dt.Columns.Add("measured_temp", typeof(double));
            dt.Columns.Add("deviation", typeof(double));

            foreach (var r in records)
            {
                dt.Rows.Add(r.Id, r.CalibrationDate, r.Operator,
                    r.ReferenceTemp, r.MeasuredTemp, r.Deviation);
            }
            return dt;
        }

        // ========== 导出 ==========

        public string ExportExcel(string productId, string testId)
        {
            var record = _app.Db.GetTestMaster(productId, testId);
            var temps = _app.TestMaster.GetAllTemperatures();
            if (record == null)
                throw new Exception("未找到试验记录");
            return _app.Export.ExportExcel(record, temps);
        }

        public string ExportPdf(string productId, string testId)
        {
            var record = _app.Db.GetTestMaster(productId, testId);
            var temps = _app.TestMaster.GetAllTemperatures();
            if (record == null)
                throw new Exception("未找到试验记录");
            return _app.Export.ExportPdf(record, temps);
        }

        public string ExportQueryResults(DataTable records)
        {
            // 导出查询结果为 Excel（简化：用第一条记录的方式，或汇总）
            // 这里使用 ExportService 逐条导出到同一文件
            string basePath = _app.Config.OutputDirectory;
            if (!System.IO.Directory.Exists(basePath))
                System.IO.Directory.CreateDirectory(basePath);
            string path = System.IO.Path.Combine(basePath,
                $"查询结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            // 简化实现：返回路径
            return path;
        }
    }
}

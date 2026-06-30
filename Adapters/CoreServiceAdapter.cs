using System;
using System.Collections.Generic;
using System.Data;
using BuildingLearn.Core;
using BuildingLearn.Data;
using BuildingLearn.Data.Models;
using BuildingLearn.Global;
using BuildingLearn.Services;
// 前端接口类型用别名区分，避免与 BuildingLearn.Core 同名类型冲突
using IDataBroadcastEventArgs = BuildingFireTest.Interfaces.DataBroadcastEventArgs;
using ITemperatureData = BuildingFireTest.Interfaces.TemperatureData;
using IMasterMessage = BuildingFireTest.Interfaces.MasterMessage;
using ITestState = BuildingFireTest.Interfaces.TestState;
using TestState = BuildingFireTest.Interfaces.TestState;
using ICoreService = BuildingFireTest.Interfaces.ICoreService;
using TestCreationInfo = BuildingFireTest.Interfaces.TestCreationInfo;
using TestPhenomenonRecord = BuildingFireTest.Interfaces.TestPhenomenonRecord;

namespace BuildingFireTest.Adapters
{
    /// <summary>
    /// ICoreService 适配器 — 桥接前端接口与后端 AppGlobal/TestMaster
    /// </summary>
    public class CoreServiceAdapter : ICoreService
    {
        private readonly AppGlobal _app;

        public CoreServiceAdapter()
        {
            _app = AppGlobal.Instance;
            _app.TestMaster.DataBroadcast += OnBackendBroadcast;
        }

        public event EventHandler<IDataBroadcastEventArgs>? DataBroadcast;

        // ========== 用户登录 ==========

        public string? Login(string role, string password)
        {
            // 角色名即为 username
            string username = role == "admin" ? "admin" : "experimenter";
            var op = _app.Db.GetOperator(username, password);
            if (op == null)
                return "密码错误，请重新输入";

            _app.CurrentOperator = op.Username;
            _app.CurrentRole = op.Role;
            _app.AddMessage($"系统初始化，操作员：{op.Username}");
            return null;
        }

        public string GetCurrentUserRole() => _app.CurrentRole;

        // ========== 试验控制 ==========

        public bool CreateNewTest(TestCreationInfo testInfo)
        {
            try
            {
                var apparatus = _app.Db.GetFirstApparatus();

                // 注入试验上下文到后端状态机
                var trial = new CurrentTrialInfo
                {
                    ProductId = testInfo.ProductId,
                    TestId = testInfo.TestId,
                    AmbientTemp = testInfo.EnvironmentTemp,
                    AmbientHumidity = testInfo.EnvironmentHumidity,
                    PreWeight = testInfo.PreWeight,
                    TestMode = testInfo.IsStandardDuration ? "Standard60Min" : "FixedDuration",
                    TargetDuration = testInfo.IsStandardDuration ? 3600 : testInfo.CustomDurationMinutes * 60,
                    ApparatusId = apparatus?.ApparatusId ?? "",
                    ApparatusName = apparatus?.ApparatusName ?? ""
                };
                _app.TestMaster.SetCurrentTrial(trial);

                // 写入数据库
                var record = new TestMasterRecord
                {
                    ProductId = testInfo.ProductId,
                    TestId = testInfo.TestId,
                    TestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Operator = _app.CurrentOperator,
                    ApparatusId = apparatus?.ApparatusId ?? "",
                    ApparatusName = apparatus?.ApparatusName ?? "",
                    AmbientTemp = testInfo.EnvironmentTemp,
                    AmbientHumidity = testInfo.EnvironmentHumidity,
                    ProductName = testInfo.ProductName,
                    Specification = testInfo.Specification,
                    Height = testInfo.Height,
                    Diameter = testInfo.Diameter,
                    PreWeight = testInfo.PreWeight,
                    TestMode = trial.TestMode,
                    TargetDuration = trial.TargetDuration,
                    ConstPowerValue = _app.Config.ConstPower
                };
                _app.Db.InsertTestMaster(record);

                // 保存样品信息
                _app.Db.InsertOrUpdateProduct(new ProductMaster
                {
                    ProductId = testInfo.ProductId,
                    ProductName = testInfo.ProductName,
                    Specification = testInfo.Specification,
                    Height = testInfo.Height,
                    Diameter = testInfo.Diameter
                });

                _app.AddMessage($"新建试验：{testInfo.ProductId} / {testInfo.TestId}");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void StartHeating()
        {
            _app.TestMaster.StartHeating();
            // 启动采集线程（800ms定时）
            _app.DaqWorker.Start();
        }

        public void StopHeating()
        {
            _app.TestMaster.StopHeating();
            _app.DaqWorker.Stop();
        }

        public void StartRecording()
        {
            _app.TestMaster.StartRecording();
        }

        public void StopRecording()
        {
            _app.TestMaster.StopRecording();
        }

        public bool SaveTestRecord(TestPhenomenonRecord record)
        {
            try
            {
                var trial = _app.TestMaster.CurrentTrial;
                if (trial == null) return false;

                string productId = trial.ProductId;
                string testId = trial.TestId;

                // 更新试验后质量
                _app.TestMaster.SetPostWeight(record.PostWeight);
                double lostWeight = trial.PreWeight - record.PostWeight;
                double lostWeightPer = trial.PreWeight > 0 ? (lostWeight / trial.PreWeight * 100.0) : 0;

                _app.Db.UpdateTestMasterPostWeight(productId, testId, record.PostWeight,
                    lostWeight, lostWeightPer,
                    record.HasFlame, record.FlameStartTime, record.FlameDuration, record.Remark);

                // 获取最终温度（从仿真器读取）
                var temps = _app.Simulator.GetTemperatures();
                double finalTF1 = temps[0], finalTF2 = temps[1], finalTS = temps[2], finalTC = temps[3];
                double deltaTF1 = finalTF1 - trial.AmbientTemp;
                double deltaTF2 = finalTF2 - trial.AmbientTemp;
                double deltaTS = finalTS - trial.AmbientTemp;
                double deltaTC = finalTC - trial.AmbientTemp;
                double deltatf = deltaTS; // 综合温升取表面温升

                _app.Db.UpdateTestMasterFinalTemps(productId, testId,
                    finalTF1, finalTF2, finalTS, finalTC,
                    deltaTF1, deltaTF2, deltaTS, deltaTC, deltatf,
                    trial.TotalTestTime, _app.TestMaster.ConstPowerValue);

                // 标记已保存
                _app.TestMaster.SetTotalTestTime(trial.TotalTestTime);
                _app.TestMaster.MarkSaved();
                _app.Db.UpdateTestMasterFlag(productId, testId, "10000000");

                _app.AddMessage("试验记录已保存");

                // 生成报告
                try
                {
                    var dbRecord = _app.Db.GetTestMaster(productId, testId);
                    var allTemps = _app.TestMaster.GetAllTemperatures();
                    if (dbRecord != null)
                    {
                        _app.Export.ExportExcel(dbRecord, allTemps);
                        if (_app.Config.EnablePdfExport)
                            _app.Export.ExportPdf(dbRecord, allTemps);
                    }
                }
                catch { /* 报告生成失败不影响主流程 */ }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ========== 状态查询 ==========

        public TestState GetCurrentState() => ConvertState(_app.TestMaster.State);

        public string GetStateText()
        {
            return _app.TestMaster.State switch
            {
                TestStates.Idle => "空闲",
                TestStates.Preparing => "升温中",
                TestStates.Ready => "就绪",
                TestStates.Recording => "记录中",
                TestStates.Complete => "完成",
                _ => "未知"
            };
        }

        public bool HasUnsavedCompleteTest() => _app.TestMaster.IsCompleteUnsaved;

        public bool HasActiveTest() => _app.TestMaster.State != TestStates.Idle;
        // ========== 校准 ==========

        public double GetCalibrationTemperature()
        {
            var temps = _app.Simulator.GetTemperatures();
            return temps[4]; // TCal
        }

        public void RecordCalibrationPoint(double standardTemp)
        {
            double measuredTemp = GetCalibrationTemperature();
            var record = new CalibrationRecord
            {
                CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Operator = _app.CurrentOperator,
                ApparatusId = _app.TestMaster.CurrentTrial?.ApparatusId ?? "",
                ReferenceTemp = standardTemp,
                MeasuredTemp = measuredTemp,
                Deviation = measuredTemp - standardTemp
            };
            _app.Db.InsertCalibrationRecord(record);
        }

        // ========== 事件转换 ==========

        private void OnBackendBroadcast(object? sender, BuildingLearn.Core.DataBroadcastEventArgs e)
        {
            var handler = DataBroadcast;
            if (handler == null) return;

            var temps = e.Temperatures;
            var uiArgs = new IDataBroadcastEventArgs
            {
                Temperature = new ITemperatureData
                {
                    TempFurnace1 = temps.Length > 0 ? temps[0] : 0,
                    TempFurnace2 = temps.Length > 1 ? temps[1] : 0,
                    TempSurface = temps.Length > 2 ? temps[2] : 0,
                    TempCenter = temps.Length > 3 ? temps[3] : 0,
                    TempCalibration = temps.Length > 4 ? temps[4] : 0,
                    TimeSeconds = e.ElapsedSeconds
                },
                CurrentState = ConvertState(e.State),
                StateText = GetStateText(),
                RecordingSeconds = e.ElapsedSeconds,
                TemperatureDrift = e.TemperatureDrift,
                ProductId = e.ProductId,
                TestId = e.TestId,
                HasUnsavedComplete = _app.TestMaster.IsCompleteUnsaved,
                Messages = new List<IMasterMessage>()
            };

            foreach (var msg in e.Messages)
            {
                uiArgs.Messages.Add(new IMasterMessage
                {
                    Time = msg.Time,
                    Message = msg.Message,
                    IsWarning = msg.Message.Contains("终止") || msg.Message.Contains("结束")
                });
            }

            handler(this, uiArgs);
        }

        private static ITestState ConvertState(TestStates backendState)
        {
            return backendState switch
            {
                TestStates.Idle => TestState.Idle,
                TestStates.Preparing => TestState.Preparing,
                TestStates.Ready => TestState.Ready,
                TestStates.Recording => TestState.Recording,
                TestStates.Complete => TestState.Complete,
                _ => TestState.Idle
            };
        }
    }
}

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Timers;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest
{
    /// <summary>
    /// ICoreService 桩实现 —— 仅供UI独立开发/测试使用
    /// 联调时替换为人员B的 TestMaster 真实实现
    /// </summary>
    public class StubCoreService : ICoreService
    {
        private TestState _state = TestState.Idle;
        private readonly System.Timers.Timer _simTimer;
        private readonly Random _rng = new();
        private double _tf1 = 25.0, _tf2 = 24.9, _ts = 24.5, _tc = 24.3, _tcal = 25.1;
        private int _recordingSeconds;
        private bool _hasUnsaved;
        private int _tickCount;

        public event EventHandler<DataBroadcastEventArgs> DataBroadcast;

        public StubCoreService()
        {
            _simTimer = new System.Timers.Timer(800); // 800ms 仿真周期
            _simTimer.Elapsed += OnSimTick;
            _simTimer.AutoReset = true;
        }

        public string Login(string role, string password)
        {
            // 桩：简单校验
            var validPasswords = new Dictionary<string, string>
            {
                ["admin"] = "123456",
                ["experimenter"] = "123456"
            };

            if (validPasswords.TryGetValue(role, out var correctPwd) && password == correctPwd)
                return null; // 登录成功

            return "密码错误，请重新输入";
        }

        public string GetCurrentUserRole() => "admin";

        public TestState GetCurrentState() => _state;

        public string GetStateText() => _state switch
        {
            TestState.Idle => "空闲",
            TestState.Preparing => "升温中",
            TestState.Ready => "就绪",
            TestState.Recording => "记录中",
            TestState.Complete => "完成",
            _ => "未知"
        };

        public bool HasUnsavedCompleteTest() => _hasUnsaved;

        public bool HasActiveTest() => _state != TestState.Idle;

        public bool CreateNewTest(TestCreationInfo testInfo)
        {
            // 桩：总是成功
            return true;
        }

        public void StartHeating()
        {
            if (_state != TestState.Idle) return;
            _state = TestState.Preparing;
            _simTimer.Start();
            BroadcastMessage("开始升温，系统升温中", false);
        }

        public void StopHeating()
        {
            if (_state != TestState.Preparing && _state != TestState.Ready && _state != TestState.Complete) return;
            _simTimer.Stop();
            _state = TestState.Idle;
            _recordingSeconds = 0;
            // 降温
            _tf1 = Math.Max(25, _tf1 - 10);
            _tf2 = Math.Max(25, _tf2 - 10);
            BroadcastMessage("停止升温，系统已空闲", false);
        }

        public void StartRecording()
        {
            if (_state != TestState.Ready) return;
            _state = TestState.Recording;
            _recordingSeconds = 0;
            BroadcastMessage("开始记录，计时开始", false);
        }

        public void StopRecording()
        {
            if (_state != TestState.Recording) return;
            _state = TestState.Complete;
            _hasUnsaved = true;
            BroadcastMessage("用户手动停止记录", false);
        }

        public bool SaveTestRecord(TestPhenomenonRecord record)
        {
            _hasUnsaved = false;
            _simTimer.Stop();
            return true;
        }

        public double GetCalibrationTemperature() => _tcal;

        public void RecordCalibrationPoint(double standardTemp)
        {
            // 桩：记录校准点
        }

        private void OnSimTick(object sender, ElapsedEventArgs e)
        {
            _tickCount++;

            // 简化的温度仿真（仅用于UI测试，真正的仿真由B层实现）
            switch (_state)
            {
                case TestState.Preparing:
                    _tf1 = Math.Min(755, _tf1 + 32 + (_rng.NextDouble() - 0.5)); // 40°C/s * 0.8
                    _tf2 = Math.Min(755, _tf2 + 32 + (_rng.NextDouble() - 0.5));
                    _ts = _tf1 * 0.3 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tc = _tf1 * 0.25 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tcal = _tf1 + (_rng.NextDouble() - 0.5) * 2;

                    // 模拟稳定判定
                    if (_tf1 >= 747 && _tickCount > 5)
                    {
                        _state = TestState.Ready;
                        _tf1 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                        _tf2 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                        BroadcastMessage("温度已稳定，可以开始记录", false);
                    }
                    break;

                case TestState.Ready:
                    _tf1 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tf2 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                    _ts = _tf1 * 0.3 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tc = _tf1 * 0.25 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tcal = _tf1 + (_rng.NextDouble() - 0.5) * 2;
                    break;

                case TestState.Recording:
                    _recordingSeconds++;
                    _tf1 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tf2 = 750 + (_rng.NextDouble() - 0.5) * 0.5;
                    double surfaceTarget = Math.Min(_tf1 * 0.95, 800);
                    _ts += (surfaceTarget - _ts) * 0.02 + (_rng.NextDouble() - 0.5) * 0.5;
                    double centerTarget = Math.Min(_tf1 * 0.85, 750);
                    _tc += (centerTarget - _tc) * 0.01 + (_rng.NextDouble() - 0.5) * 0.5;
                    _tcal = _tf1 + (_rng.NextDouble() - 0.5) * 2;

                    // 模拟3600秒自动终止
                    if (_recordingSeconds >= 3600)
                    {
                        _state = TestState.Complete;
                        _hasUnsaved = true;
                        BroadcastMessage("记录时间到达3600秒，试验自动结束", false);
                    }
                    break;
            }

            // 广播数据
            DataBroadcast?.Invoke(this, new DataBroadcastEventArgs
            {
                Temperature = new TemperatureData
                {
                    TempFurnace1 = Math.Round(_tf1, 1),
                    TempFurnace2 = Math.Round(_tf2, 1),
                    TempSurface = Math.Round(_ts, 1),
                    TempCenter = Math.Round(_tc, 1),
                    TempCalibration = Math.Round(_tcal, 1),
                    TimeSeconds = _recordingSeconds
                },
                CurrentState = _state,
                StateText = GetStateText(),
                RecordingSeconds = _recordingSeconds,
                TemperatureDrift = Math.Round((_rng.NextDouble() - 0.5) * 2, 2),
                ProductId = "DEMO-001",
                TestId = "T001",
                Messages = _pendingMessages,
                HasUnsavedComplete = _hasUnsaved
            });

            _pendingMessages.Clear();
        }

        private readonly List<MasterMessage> _pendingMessages = new();

        private void BroadcastMessage(string text, bool isWarning)
        {
            _pendingMessages.Add(new MasterMessage
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = text,
                IsWarning = isWarning
            });
        }
    }
}
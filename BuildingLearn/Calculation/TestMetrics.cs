using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingLearn.Services;

/// <summary>
/// 试验指标计算 — 失重率、温升、deltatf、温漂回归
/// </summary>
public class TestMetrics
{
    /// <summary>
    /// 计算失重量
    /// </summary>
    public static double ComputeLostWeight(double preWeight, double postWeight)
    {
        return preWeight - postWeight;
    }

    /// <summary>
    /// 计算失重率 (%)
    /// </summary>
    public static double ComputeLostWeightPercent(double preWeight, double postWeight)
    {
        if (preWeight <= 0) return 0;
        return (preWeight - postWeight) / preWeight * 100.0;
    }

    /// <summary>
    /// 计算温升 (最终温度 - 环境温度)
    /// </summary>
    public static double ComputeTemperatureRise(double finalTemp, double ambientTemp)
    {
        return finalTemp - ambientTemp;
    }

    /// <summary>
    /// 综合样品温升 deltatf — 当前取表面温升
    /// </summary>
    public static double ComputeDeltaTF(double deltaTS, double deltaTC)
    {
        return deltaTS; // 当前代码口径：取表面温升
    }

    /// <summary>
    /// 温漂回归结果
    /// </summary>
    public struct DriftResult
    {
        public bool valid;
        public double slopeCPer10Min;   // 温漂 °C/10min
        public double intercept;
    }

    /// <summary>
    /// 对最近 10 分钟（600 个数据点）的炉温序列做线性回归，计算温漂。
    /// 使用最小二乘法手工实现，不依赖 MathNet 具体 API。
    /// </summary>
    /// <param name="history">温度历史列表，每个元素 double[5]</param>
    /// <param name="windowSeconds">窗口大小（秒），默认 600 秒</param>
    public static DriftResult ComputeDrift(List<double[]> history, int windowSeconds = 600)
    {
        var result = new DriftResult { valid = false };

        if (history == null || history.Count < 30)
            return result;

        // 取最近 windowSeconds 个数据点
        int count = Math.Min(windowSeconds, history.Count);
        var recent = history.Skip(history.Count - count).ToList();

        if (recent.Count < 30)
            return result;

        // 最小二乘法线性回归：对 TF1（index=0）做 y = a + b*x
        // b = (n*Σxy - Σx*Σy) / (n*Σx² - (Σx)²)
        // a = (Σy - b*Σx) / n
        int n = recent.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++)
        {
            double x = i;
            double y = recent[i][0]; // TF1
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-12)
            return result;

        double slope = (n * sumXY - sumX * sumY) / denominator;  // °C/数据点
        double intercept = (sumY - slope * sumX) / n;

        // 按 1 数据点/秒 折算 → slope 即 °C/秒，×600 得 °C/10min
        double slopePer10Min = slope * 600;

        result.valid = true;
        result.slopeCPer10Min = slopePer10Min;
        result.intercept = intercept;

        return result;
    }

    /// <summary>
    /// 判定结论：按 deltatf <= 50、lostweight_per <= 50、flameduration < 5
    /// </summary>
    public static string ComputeVerdict(double deltatf, double lostWeightPer, int flameDuration)
    {
        if (deltatf <= 50 && lostWeightPer <= 50 && flameDuration < 5)
            return "通过";
        else
            return "不通过";
    }
}

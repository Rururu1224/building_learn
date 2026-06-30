using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ISO11820WinForms.DB
{
    public class DbHelper
    {
        private readonly string _connStr;

        public DbHelper(string dbPath)
        {
            _connStr = $"Data Source={dbPath}";
        }

        private SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(_connStr);
            conn.Open();
            return conn;
        }

        // ============================================================
        // EnsureDatabase — 自动建库建表+初始数据（幂等安全）
        // 将完整SQL脚本一次性提交给SQLite执行，SQLite原生处理多语句+注释
        // ============================================================

        /// <summary>
        /// 确保数据库已创建且所有表结构完整。
        /// 每次启动执行，IF NOT EXISTS 保证幂等不报错。
        /// </summary>
        public static void EnsureDatabase(string dbPath)
        {
            string? dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            // 一次性执行完整建库脚本（SQLite原生支持多语句+注释）
            using var cmd = conn.CreateCommand();
            cmd.CommandText = GetInitSql();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 建库SQL — 6张表 + 初始数据
        /// 使用 C# 11 """ 原始多行字符串，纯静态SQL，无$插值
        /// </summary>
        private static string GetInitSql()
        {
            return """
                -- ============================================================
                -- ISO11820 建材不燃性试验仿真系统 — 建库脚本
                -- ============================================================

                -- 1. operators 表（操作员/用户账号）
                CREATE TABLE IF NOT EXISTS "operators" (
                    "userid"    TEXT NOT NULL,
                    "username"  TEXT NOT NULL,
                    "pwd"       TEXT NOT NULL,
                    "usertype"  TEXT NOT NULL
                );

                -- 2. apparatus 表（试验设备信息）
                CREATE TABLE IF NOT EXISTS "apparatus" (
                    "apparatusid"   INTEGER NOT NULL CONSTRAINT "PK_apparatus" PRIMARY KEY,
                    "innernumber"   TEXT NOT NULL,
                    "apparatusname" TEXT NOT NULL,
                    "checkdatef"    date NOT NULL,
                    "checkdatet"    date NOT NULL,
                    "pidport"       TEXT NOT NULL,
                    "powerport"     TEXT NOT NULL,
                    "constpower"    INTEGER NULL
                );

                -- 3. productmaster 表（样品信息）
                CREATE TABLE IF NOT EXISTS "productmaster" (
                    "productid"   TEXT NOT NULL CONSTRAINT "PK_productmaster" PRIMARY KEY,
                    "productname" TEXT NOT NULL,
                    "specific"    TEXT NOT NULL,
                    "diameter"    REAL NOT NULL,
                    "height"      REAL NOT NULL,
                    "flag"        TEXT NULL
                );

                -- 4. testmaster 表（试验记录 — 核心表）
                CREATE TABLE IF NOT EXISTS "testmaster" (
                    "productid"        TEXT NOT NULL,
                    "testid"           TEXT NOT NULL,
                    "testdate"         date NOT NULL,
                    "ambtemp"          REAL NOT NULL,
                    "ambhumi"          REAL NOT NULL,
                    "according"        TEXT NOT NULL,
                    "operator"         TEXT NOT NULL,
                    "apparatusid"      TEXT NOT NULL,
                    "apparatusname"    TEXT NOT NULL,
                    "apparatuschkdate" date NOT NULL,
                    "rptno"            TEXT NOT NULL,
                    "preweight"        REAL NOT NULL,
                    "postweight"       REAL NOT NULL,
                    "lostweight"       REAL NOT NULL,
                    "lostweight_per"   REAL NOT NULL,
                    "totaltesttime"    INTEGER NOT NULL,
                    "constpower"       INTEGER NOT NULL,
                    "phenocode"        TEXT NOT NULL,
                    "flametime"        INTEGER NOT NULL,
                    "flameduration"    INTEGER NOT NULL,
                    "maxtf1"           REAL NOT NULL,
                    "maxtf2"           REAL NOT NULL,
                    "maxts"            REAL NOT NULL,
                    "maxtc"            REAL NOT NULL,
                    "maxtf1_time"      INTEGER NOT NULL,
                    "maxtf2_time"      INTEGER NOT NULL,
                    "maxts_time"       INTEGER NOT NULL,
                    "maxtc_time"       INTEGER NOT NULL,
                    "finaltf1"         REAL NOT NULL,
                    "finaltf2"         REAL NOT NULL,
                    "finalts"          REAL NOT NULL,
                    "finaltc"          REAL NOT NULL,
                    "finaltf1_time"    INTEGER NOT NULL,
                    "finaltf2_time"    INTEGER NOT NULL,
                    "finalts_time"     INTEGER NOT NULL,
                    "finaltc_time"     INTEGER NOT NULL,
                    "deltatf1"         REAL NOT NULL,
                    "deltatf2"         REAL NOT NULL,
                    "deltatf"          REAL NOT NULL,
                    "deltats"          REAL NOT NULL,
                    "deltatc"          REAL NOT NULL,
                    "memo"             TEXT NULL,
                    "flag"             TEXT NULL,
                    CONSTRAINT "PK_testmaster" PRIMARY KEY ("productid", "testid"),
                    CONSTRAINT "FK_testmaster_productmaster" FOREIGN KEY ("productid") REFERENCES "productmaster" ("productid")
                );

                CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate"            ON "testmaster" ("testdate");
                CREATE INDEX IF NOT EXISTS "IX_Testmaster_Operator"            ON "testmaster" ("operator");
                CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate_Productid"  ON "testmaster" ("testdate", "productid");

                -- 5. sensors 表（传感器通道配置）
                CREATE TABLE IF NOT EXISTS "sensors" (
                    "sensorid"    INTEGER NOT NULL CONSTRAINT "PK_sensors" PRIMARY KEY,
                    "sensorname"  TEXT NOT NULL,
                    "dispname"    TEXT NOT NULL,
                    "sensorgroup" TEXT NOT NULL,
                    "unit"        TEXT NOT NULL,
                    "discription" TEXT NOT NULL,
                    "flag"        TEXT NOT NULL,
                    "signalzero"  REAL NOT NULL,
                    "signalspan"  REAL NOT NULL,
                    "outputzero"  REAL NOT NULL,
                    "outputspan"  REAL NOT NULL,
                    "outputvalue" REAL NOT NULL,
                    "inputvalue"  REAL NOT NULL,
                    "signaltype"  INTEGER NOT NULL
                );

                -- 6. CalibrationRecords 表（设备校准历史）
                CREATE TABLE IF NOT EXISTS "CalibrationRecords" (
                    "Id"                 TEXT NOT NULL CONSTRAINT "PK_CalibrationRecords" PRIMARY KEY,
                    "CalibrationDate"    TEXT NOT NULL,
                    "CalibrationType"    TEXT NOT NULL,
                    "ApparatusId"        INTEGER NOT NULL,
                    "Operator"           TEXT NOT NULL,
                    "TemperatureData"    TEXT NOT NULL,
                    "UniformityResult"   REAL NULL,
                    "MaxDeviation"       REAL NULL,
                    "AverageTemperature" REAL NULL,
                    "PassedCriteria"     INTEGER NOT NULL,
                    "Remarks"            TEXT NOT NULL,
                    "CreatedAt"          TEXT NOT NULL,
                    "TempA1" REAL NULL, "TempA2" REAL NULL, "TempA3" REAL NULL,
                    "TempB1" REAL NULL, "TempB2" REAL NULL, "TempB3" REAL NULL,
                    "TempC1" REAL NULL, "TempC2" REAL NULL, "TempC3" REAL NULL,
                    "TAvg"        REAL NULL,
                    "TAvgAxis1"   REAL NULL, "TAvgAxis2" REAL NULL, "TAvgAxis3" REAL NULL,
                    "TAvgLevela"  REAL NULL, "TAvgLevelb" REAL NULL, "TAvgLevelc" REAL NULL,
                    "TDevAxis1"   REAL NULL, "TDevAxis2" REAL NULL, "TDevAxis3" REAL NULL,
                    "TDevLevela"  REAL NULL, "TDevLevelb" REAL NULL, "TDevLevelc" REAL NULL,
                    "TAvgDevAxis" REAL NULL, "TAvgDevLevel" REAL NULL,
                    "CenterTempData" TEXT NULL,
                    "Memo"           TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Date"     ON "CalibrationRecords" ("CalibrationDate");
                CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Operator" ON "CalibrationRecords" ("Operator");

                -- ============================================================
                -- 初始数据（WHERE NOT EXISTS 保证幂等）
                -- ============================================================

                INSERT INTO operators (userid, username, pwd, usertype)
                SELECT '1', 'admin', '123456', 'admin'
                WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');

                INSERT INTO operators (userid, username, pwd, usertype)
                SELECT '2', 'experimenter', '123456', 'operator'
                WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');

                INSERT INTO apparatus (apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower)
                SELECT 0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
                WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0);

                INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
                SELECT 0, 'Sensor0', '炉温1', '采集', '℃', '炉温1', '启用', 0, 0, 0, 1000, 0, 0, 4
                WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 0);

                INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
                SELECT 1, 'Sensor1', '炉温2', '采集', '℃', '炉温2', '启用', 0, 0, 0, 1000, 0, 0, 4
                WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 1);

                INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
                SELECT 2, 'Sensor2', '表面温度', '采集', '℃', '表面温度', '启用', 0, 0, 0, 1000, 0, 0, 4
                WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 2);

                INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
                SELECT 3, 'Sensor3', '中心温度', '采集', '℃', '中心温度', '启用', 0, 0, 0, 1000, 0, 0, 4
                WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 3);

                INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
                SELECT 16, 'Sensor16', '校准温度', '校准', '℃', '校准温度', '启用', 0, 0, 0, 1000, 0, 0, 4
                WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = 16);
                """;
        }

        // ============================================================
        // 登录验证
        // ============================================================

        public bool Login(string username, string pwd, out string usertype)
        {
            usertype = string.Empty;
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT usertype FROM operators WHERE username=$name AND pwd=$pwd";
            cmd.Parameters.AddWithValue("$name", username);
            cmd.Parameters.AddWithValue("$pwd", pwd);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                usertype = reader.GetString(0);
                return true;
            }
            return false;
        }

        // ============================================================
        // operators 表操作
        // ============================================================

        public List<(string Username, string Usertype)> GetOperators()
        {
            var list = new List<(string, string)>();
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username, usertype FROM operators";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add((reader.GetString(0), reader.GetString(1)));
            return list;
        }

        // ============================================================
        // apparatus 表操作
        // ============================================================

        public (int Id, string InnerNum, string Name, string CheckF, string CheckT, string PidPort, string PowerPort, int? ConstPower) GetApparatus()
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower FROM apparatus LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7)
                );
            }
            return (0, "", "", "", "", "", "", null);
        }

        // ============================================================
        // productmaster 表操作
        // ============================================================

        public void InsertProduct(string productId, string name, string specific, double diameter, double height)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO productmaster (productid, productname, specific, diameter, height)
                VALUES ($pid, $name, $spec, $diam, $height)
                """;
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$spec", specific);
            cmd.Parameters.AddWithValue("$diam", diameter);
            cmd.Parameters.AddWithValue("$height", height);
            cmd.ExecuteNonQuery();
        }

        public (string Name, string Specific, double Diameter, double Height)? GetProduct(string productId)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT productname, specific, diameter, height FROM productmaster WHERE productid=$pid";
            cmd.Parameters.AddWithValue("$pid", productId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetString(0), reader.GetString(1),
                        reader.GetDouble(2), reader.GetDouble(3));
            }
            return null;
        }

        // ============================================================
        // testmaster 表操作（核心表）
        // ============================================================

        public void InsertTestMaster(
            string productId, string testId, string testDate,
            double ambtemp, double ambhumi, string according,
            string operatorName, string apparatusId, string apparatusName,
            string apparatusChkDate, string rptNo, double preWeight)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO testmaster (
                    productid, testid, testdate, ambtemp, ambhumi, according,
                    operator, apparatusid, apparatusname, apparatuschkdate, rptno,
                    preweight, postweight, lostweight, lostweight_per,
                    totaltesttime, constpower, phenocode, flametime, flameduration,
                    maxtf1,maxtf2,maxts,maxtc,
                    maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
                    finaltf1,finaltf2,finalts,finaltc,
                    finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
                    deltatf1,deltatf2,deltatf,deltats,deltatc
                ) VALUES (
                    $pid,$tid,$tdate,$ambtemp,$ambhumi,$acc,
                    $op,$apid,$apname,$apchkdate,$rptno,
                    $prewt,0,0,0,
                    0,0,'',0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0)
                """;
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            cmd.Parameters.AddWithValue("$tdate", testDate);
            cmd.Parameters.AddWithValue("$ambtemp", ambtemp);
            cmd.Parameters.AddWithValue("$ambhumi", ambhumi);
            cmd.Parameters.AddWithValue("$acc", according);
            cmd.Parameters.AddWithValue("$op", operatorName);
            cmd.Parameters.AddWithValue("$apid", apparatusId);
            cmd.Parameters.AddWithValue("$apname", apparatusName);
            cmd.Parameters.AddWithValue("$apchkdate", apparatusChkDate);
            cmd.Parameters.AddWithValue("$rptno", rptNo);
            cmd.Parameters.AddWithValue("$prewt", preWeight);
            cmd.ExecuteNonQuery();
        }

        public void UpdateTestResult(
            string productId, string testId,
            double postWeight, double lostWeight, double lostWeightPer,
            double maxtf1, double maxtf2, double maxts, double maxtc,
            int maxtf1Time, int maxtf2Time, int maxtsTime, int maxtcTime,
            double finaltf1, double finaltf2, double finalts, double finaltc,
            int finaltf1Time, int finaltf2Time, int finaltsTime, int finaltcTime,
            double deltatf1, double deltatf2, double deltatf, double deltats, double deltatc,
            int totalTestTime, int constPower, string phenoCode,
            int flameTime, int flameDuration, string memo)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE testmaster SET
                    postweight=$post, lostweight=$lost, lostweight_per=$lostper,
                    maxtf1=$mt1, maxtf2=$mt2, maxts=$mts, maxtc=$mtc,
                    maxtf1_time=$mt1t, maxtf2_time=$mt2t, maxts_time=$mtst, maxtc_time=$mtct,
                    finaltf1=$ft1, finaltf2=$ft2, finalts=$fts, finaltc=$ftc,
                    finaltf1_time=$ft1t, finaltf2_time=$ft2t, finalts_time=$ftst, finaltc_time=$ftct,
                    deltatf1=$dt1, deltatf2=$dt2, deltatf=$dtf, deltats=$dts, deltatc=$dtc,
                    totaltesttime=$ttt, constpower=$cp,
                    phenocode=$pheno, flametime=$ftime, flameduration=$fdur,
                    memo=$memo, flag='10000000'
                WHERE productid=$pid AND testid=$tid
                """;
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            cmd.Parameters.AddWithValue("$post", postWeight);
            cmd.Parameters.AddWithValue("$lost", lostWeight);
            cmd.Parameters.AddWithValue("$lostper", lostWeightPer);
            cmd.Parameters.AddWithValue("$mt1", maxtf1);
            cmd.Parameters.AddWithValue("$mt2", maxtf2);
            cmd.Parameters.AddWithValue("$mts", maxts);
            cmd.Parameters.AddWithValue("$mtc", maxtc);
            cmd.Parameters.AddWithValue("$mt1t", maxtf1Time);
            cmd.Parameters.AddWithValue("$mt2t", maxtf2Time);
            cmd.Parameters.AddWithValue("$mtst", maxtsTime);
            cmd.Parameters.AddWithValue("$mtct", maxtcTime);
            cmd.Parameters.AddWithValue("$ft1", finaltf1);
            cmd.Parameters.AddWithValue("$ft2", finaltf2);
            cmd.Parameters.AddWithValue("$fts", finalts);
            cmd.Parameters.AddWithValue("$ftc", finaltc);
            cmd.Parameters.AddWithValue("$ft1t", finaltf1Time);
            cmd.Parameters.AddWithValue("$ft2t", finaltf2Time);
            cmd.Parameters.AddWithValue("$ftst", finaltsTime);
            cmd.Parameters.AddWithValue("$ftct", finaltcTime);
            cmd.Parameters.AddWithValue("$dt1", deltatf1);
            cmd.Parameters.AddWithValue("$dt2", deltatf2);
            cmd.Parameters.AddWithValue("$dtf", deltatf);
            cmd.Parameters.AddWithValue("$dts", deltats);
            cmd.Parameters.AddWithValue("$dtc", deltatc);
            cmd.Parameters.AddWithValue("$ttt", totalTestTime);
            cmd.Parameters.AddWithValue("$cp", constPower);
            cmd.Parameters.AddWithValue("$pheno", phenoCode);
            cmd.Parameters.AddWithValue("$ftime", flameTime);
            cmd.Parameters.AddWithValue("$fdur", flameDuration);
            cmd.Parameters.AddWithValue("$memo", memo ?? "");
            cmd.ExecuteNonQuery();
        }

        public List<Dictionary<string, object>> QueryTestHistory(DateTime from, DateTime to, string productId = "", string operatorName = "")
        {
            var list = new List<Dictionary<string, object>>();
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT productid, testid, testdate, operator, ambtemp, ambhumi,
                       preweight, postweight, lostweight_per, totaltesttime, deltatf,
                       phenocode, flametime, flameduration, apparatusname, flag
                FROM testmaster
                WHERE testdate BETWEEN $from AND $to
                  AND ($pid = '' OR productid LIKE '%' || $pid || '%')
                  AND ($op = '' OR operator = $op)
                ORDER BY testdate DESC
                """;
            cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$pid", productId ?? "");
            cmd.Parameters.AddWithValue("$op", operatorName ?? "");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>
                {
                    ["productid"] = reader.GetString(0),
                    ["testid"] = reader.GetString(1),
                    ["testdate"] = reader.GetString(2),
                    ["operator"] = reader.GetString(3),
                    ["ambtemp"] = reader.GetDouble(4),
                    ["ambhumi"] = reader.GetDouble(5),
                    ["preweight"] = reader.GetDouble(6),
                    ["postweight"] = reader.GetDouble(7),
                    ["lostweight_per"] = reader.GetDouble(8),
                    ["totaltesttime"] = reader.GetInt32(9),
                    ["deltatf"] = reader.GetDouble(10),
                    ["phenocode"] = reader.GetString(11),
                    ["flametime"] = reader.GetInt32(12),
                    ["flameduration"] = reader.GetInt32(13),
                    ["apparatusname"] = reader.GetString(14),
                    ["flag"] = reader.IsDBNull(15) ? null : reader.GetString(15)
                };
                list.Add(row);
            }
            return list;
        }

        public Dictionary<string, object>? GetTestById(string productId, string testId)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            }
            return row;
        }

        public bool HasUnsavedCompletedTest()
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*) FROM testmaster
                WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000')
                """;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public Dictionary<string, object>? GetLatestUnsavedTest()
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT * FROM testmaster
                WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000')
                ORDER BY rowid DESC LIMIT 1
                """;
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            }
            return row;
        }

        // ============================================================
        // sensors 表操作
        // ============================================================

        public List<Dictionary<string, object>> GetAllSensors()
        {
            var list = new List<Dictionary<string, object>>();
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM sensors ORDER BY sensorid";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                list.Add(row);
            }
            return list;
        }

        // ============================================================
        // CalibrationRecords 表操作
        // ============================================================

        public void InsertCalibrationRecord(
            string id, string calDate, string calType, int apparatusId,
            string operatorName, string temperatureDataJson,
            double? uniformityResult, double? maxDeviation, double? avgTemperature,
            int passedCriteria, string remarks, string createdAt,
            double? tempA1, double? tempA2, double? tempA3,
            double? tempB1, double? tempB2, double? tempB3,
            double? tempC1, double? tempC2, double? tempC3,
            double? tAvg, double? tAvgAxis1, double? tAvgAxis2, double? tAvgAxis3,
            double? tAvgLevela, double? tAvgLevelb, double? tAvgLevelc,
            double? tDevAxis1, double? tDevAxis2, double? tDevAxis3,
            double? tDevLevela, double? tDevLevelb, double? tDevLevelc,
            double? tAvgDevAxis, double? tAvgDevLevel,
            string? centerTempDataJson, string? memo)
        {
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO CalibrationRecords (
                    Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
                    TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
                    PassedCriteria, Remarks, CreatedAt,
                    TempA1,TempA2,TempA3,TempB1,TempB2,TempB3,TempC1,TempC2,TempC3,
                    TAvg,TAvgAxis1,TAvgAxis2,TAvgAxis3,
                    TAvgLevela,TAvgLevelb,TAvgLevelc,
                    TDevAxis1,TDevAxis2,TDevAxis3,
                    TDevLevela,TDevLevelb,TDevLevelc,
                    TAvgDevAxis,TAvgDevLevel,
                    CenterTempData, Memo
                ) VALUES (
                    $id,$caldate,$caltype,$apid,$op,
                    $tempdata,$uniform,$maxdev,$avgtemp,
                    $pass,$remarks,$createdat,
                    $a1,$a2,$a3,$b1,$b2,$b3,$c1,$c2,$c3,
                    $tavg,$tavg1,$tavg2,$tavg3,
                    $tavgLa,$tavgLb,$tavgLc,
                    $tdev1,$tdev2,$tdev3,
                    $tdevLa,$tdevLb,$tdevLc,
                    $tavgDevA,$tavgDevL,
                    $ctdata,$memo)
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$caldate", calDate);
            cmd.Parameters.AddWithValue("$caltype", calType);
            cmd.Parameters.AddWithValue("$apid", apparatusId);
            cmd.Parameters.AddWithValue("$op", operatorName);
            cmd.Parameters.AddWithValue("$tempdata", temperatureDataJson);
            cmd.Parameters.AddWithValue("$uniform", (object?)uniformityResult ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$maxdev", (object?)maxDeviation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$avgtemp", (object?)avgTemperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pass", passedCriteria);
            cmd.Parameters.AddWithValue("$remarks", remarks);
            cmd.Parameters.AddWithValue("$createdat", createdAt);
            cmd.Parameters.AddWithValue("$a1", (object?)tempA1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a2", (object?)tempA2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a3", (object?)tempA3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$b1", (object?)tempB1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$b2", (object?)tempB2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$b3", (object?)tempB3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$c1", (object?)tempC1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$c2", (object?)tempC2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$c3", (object?)tempC3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavg", (object?)tAvg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavg1", (object?)tAvgAxis1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavg2", (object?)tAvgAxis2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavg3", (object?)tAvgAxis3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavgLa", (object?)tAvgLevela ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavgLb", (object?)tAvgLevelb ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavgLc", (object?)tAvgLevelc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdev1", (object?)tDevAxis1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdev2", (object?)tDevAxis2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdev3", (object?)tDevAxis3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdevLa", (object?)tDevLevela ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdevLb", (object?)tDevLevelb ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tdevLc", (object?)tDevLevelc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavgDevA", (object?)tAvgDevAxis ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tavgDevL", (object?)tAvgDevLevel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ctdata", (object?)centerTempDataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$memo", (object?)memo ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<Dictionary<string, object>> GetCalibrationRecords()
        {
            var list = new List<Dictionary<string, object>>();
            using var conn = CreateConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CalibrationRecords ORDER BY CreatedAt DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                list.Add(row);
            }
            return list;
        }
    }
}

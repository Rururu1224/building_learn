using Microsoft.Data.Sqlite;
using BuildingLearn.Data.Models;
using BuildingLearn.Services;
using Serilog;

namespace BuildingLearn.Data;

/// <summary>
/// SQLite 数据库封装 —— 建表、CRUD 操作
/// </summary>
public class DbHelper : IDisposable
{
    private readonly string _connectionString;
    private readonly ConfigService _config;

    public DbHelper(ConfigService config)
    {
        _config = config;
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.SqlitePath);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
        Log.Information("数据库初始化完成: {Path}", dbPath);
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void InitializeDatabase()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        // ===== 1. operators =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS operators (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                userid TEXT NOT NULL,
                username TEXT NOT NULL,
                pwd TEXT NOT NULL,
                role TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();

        // ===== 2. productmaster =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS productmaster (
                productid TEXT PRIMARY KEY,
                productname TEXT NOT NULL,
                specification TEXT,
                height REAL,
                diameter REAL,
                createtime TEXT
            );";
        cmd.ExecuteNonQuery();

        // ===== 3. apparatus =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS apparatus (
                apparatusid TEXT PRIMARY KEY,
                apparatusname TEXT NOT NULL,
                comport TEXT,
                baudrate INTEGER DEFAULT 9600,
                constpower INTEGER DEFAULT 2048,
                calibrationdate TEXT,
                nextcalibrationdate TEXT
            );";
        cmd.ExecuteNonQuery();

        // ===== 4. sensors =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS sensors (
                channelid INTEGER PRIMARY KEY,
                channelname TEXT NOT NULL,
                rangemin REAL,
                rangemax REAL,
                unit TEXT DEFAULT '°C',
                modbusaddress TEXT
            );";
        cmd.ExecuteNonQuery();

        // ===== 5. testmaster =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS testmaster (
                productid TEXT NOT NULL,
                testid TEXT NOT NULL,
                testdate TEXT,
                operator TEXT,
                apparatusid TEXT,
                apparatusname TEXT,
                ambienttemp REAL,
                ambienthumidity REAL,
                productname TEXT,
                specification TEXT,
                height REAL,
                diameter REAL,
                preweight REAL,
                postweight REAL,
                lostweight REAL,
                lostweight_per REAL,
                finaltf1 REAL,
                finaltf2 REAL,
                finalts REAL,
                finaltc REAL,
                deltatf REAL,
                deltatf1 REAL,
                deltatf2 REAL,
                deltats REAL,
                deltatc REAL,
                hasflame INTEGER DEFAULT 0,
                flamestarttime INTEGER DEFAULT 0,
                flameduration INTEGER DEFAULT 0,
                testmode TEXT DEFAULT 'Standard60Min',
                totaltesttime INTEGER DEFAULT 0,
                targetduration INTEGER DEFAULT 3600,
                constpowervalue INTEGER DEFAULT 0,
                flag TEXT DEFAULT '00000000',
                remark TEXT,
                calibrationdate TEXT,
                datafilepath TEXT,
                PRIMARY KEY (productid, testid)
            );";
        cmd.ExecuteNonQuery();

        // ===== 6. calibrationrecords =====
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS calibrationrecords (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                calibrationdate TEXT,
                operator TEXT,
                apparatusid TEXT,
                referencetemp REAL,
                measuredtemp REAL,
                deviation REAL,
                remark TEXT
            );";
        cmd.ExecuteNonQuery();

        // ===== 插入默认数据 =====
        SeedDefaultData(conn);
    }

    private void SeedDefaultData(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();

        // 默认操作员
        cmd.CommandText = "SELECT COUNT(*) FROM operators;";
        if ((long)cmd.ExecuteScalar()! == 0)
        {
            cmd.CommandText = @"
                INSERT INTO operators (userid, username, pwd, role) VALUES
                ('admin', 'admin', '123456', 'admin'),
                ('experimenter', 'experimenter', '123456', 'experimenter');";
            cmd.ExecuteNonQuery();
        }

        // 默认设备
        cmd.CommandText = "SELECT COUNT(*) FROM apparatus;";
        if ((long)cmd.ExecuteScalar()! == 0)
        {
            cmd.CommandText = @"
                INSERT INTO apparatus (apparatusid, apparatusname, comport, baudrate, constpower, calibrationdate, nextcalibrationdate) VALUES
                ('ISO-001', '不燃性试验炉 ISO11820', 'COM3', 9600, 2048, '2026-01-15', '2027-01-15');";
            cmd.ExecuteNonQuery();
        }

        // 默认传感器
        cmd.CommandText = "SELECT COUNT(*) FROM sensors;";
        if ((long)cmd.ExecuteScalar()! == 0)
        {
            cmd.CommandText = @"
                INSERT INTO sensors (channelid, channelname, rangemin, rangemax, unit, modbusaddress) VALUES
                (1, '炉温1 TF1', 0, 1200, '°C', '40001'),
                (2, '炉温2 TF2', 0, 1200, '°C', '40002'),
                (3, '表面温 TS',  0, 1200, '°C', '40003'),
                (4, '中心温 TC',  0, 1200, '°C', '40004'),
                (5, '校准温 TCal',0, 1200, '°C', '40005');";
            cmd.ExecuteNonQuery();
        }
    }

    // ==================== 通用查询 ====================

    public List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, Dictionary<string, object>? parameters = null)
    {
        var result = new List<T>();
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameters != null)
        {
            foreach (var kv in parameters)
                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(map(reader));
        return result;
    }

    public T? QuerySingle<T>(string sql, Func<SqliteDataReader, T> map, Dictionary<string, object>? parameters = null)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameters != null)
        {
            foreach (var kv in parameters)
                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return map(reader);
        return default;
    }

    public int Execute(string sql, Dictionary<string, object>? parameters = null)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameters != null)
        {
            foreach (var kv in parameters)
                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
        return cmd.ExecuteNonQuery();
    }

    public long ExecuteInsert(string sql, Dictionary<string, object>? parameters = null)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameters != null)
        {
            foreach (var kv in parameters)
                cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
        cmd.ExecuteNonQuery();

        // 获取最后插入的 rowid
        cmd.CommandText = "SELECT last_insert_rowid();";
        return (long)cmd.ExecuteScalar()!;
    }

    // ==================== 操作员 ====================

    public Operator? GetOperator(string username, string password)
    {
        var sql = "SELECT * FROM operators WHERE username = @u AND pwd = @p LIMIT 1;";
        return QuerySingle(sql, r => new Operator
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            UserId = r.GetString(r.GetOrdinal("userid")),
            Username = r.GetString(r.GetOrdinal("username")),
            Pwd = r.GetString(r.GetOrdinal("pwd")),
            Role = r.GetString(r.GetOrdinal("role")),
        }, new() { ["@u"] = username, ["@p"] = password });
    }

    // ==================== 试验操作 ====================

    public void InsertTestMaster(TestMasterRecord record)
    {
        var sql = @"
            INSERT OR REPLACE INTO testmaster (
                productid, testid, testdate, operator, apparatusid, apparatusname,
                ambienttemp, ambienthumidity,
                productname, specification, height, diameter,
                preweight, postweight, lostweight, lostweight_per,
                finaltf1, finaltf2, finalts, finaltc,
                deltatf, deltatf1, deltatf2, deltats, deltatc,
                hasflame, flamestarttime, flameduration,
                testmode, totaltesttime, targetduration, constpowervalue,
                flag, remark, calibrationdate, datafilepath
            ) VALUES (
                @pid, @tid, @tdate, @oper, @aid, @aname,
                @atemp, @ahum,
                @pname, @spec, @h, @d,
                @pw, @pw2, @lw, @lwp,
                @ftf1, @ftf2, @fts, @ftc,
                @dtf, @dtf1, @dtf2, @dts, @dtc,
                @hf, @fst, @fd,
                @tm, @ttt, @td, @cpv,
                @flag, @remark, @caldate, @dfp
            );";

        Execute(sql, new()
        {
            ["@pid"] = record.ProductId,
            ["@tid"] = record.TestId,
            ["@tdate"] = record.TestDate,
            ["@oper"] = record.Operator,
            ["@aid"] = record.ApparatusId,
            ["@aname"] = record.ApparatusName,
            ["@atemp"] = record.AmbientTemp,
            ["@ahum"] = record.AmbientHumidity,
            ["@pname"] = record.ProductName,
            ["@spec"] = record.Specification,
            ["@h"] = record.Height,
            ["@d"] = record.Diameter,
            ["@pw"] = record.PreWeight,
            ["@pw2"] = record.PostWeight,
            ["@lw"] = record.LostWeight,
            ["@lwp"] = record.LostWeightPer,
            ["@ftf1"] = record.FinalTF1,
            ["@ftf2"] = record.FinalTF2,
            ["@fts"] = record.FinalTS,
            ["@ftc"] = record.FinalTC,
            ["@dtf"] = record.Deltatf,
            ["@dtf1"] = record.DeltaTF1,
            ["@dtf2"] = record.DeltaTF2,
            ["@dts"] = record.DeltaTS,
            ["@dtc"] = record.DeltaTC,
            ["@hf"] = record.HasFlame ? 1 : 0,
            ["@fst"] = record.FlameStartTime,
            ["@fd"] = record.FlameDuration,
            ["@tm"] = record.TestMode,
            ["@ttt"] = record.TotalTestTime,
            ["@td"] = record.TargetDuration,
            ["@cpv"] = record.ConstPowerValue,
            ["@flag"] = record.Flag,
            ["@remark"] = record.Remark,
            ["@caldate"] = record.CalibrationDate.ToString("yyyy-MM-dd"),
            ["@dfp"] = record.DataFilePath,
        });
    }

    public TestMasterRecord? GetTestMaster(string productId, string testId)
    {
        var sql = "SELECT * FROM testmaster WHERE productid = @pid AND testid = @tid LIMIT 1;";
        return QuerySingle(sql, MapTestMaster, new() { ["@pid"] = productId, ["@tid"] = testId });
    }

    public List<TestMasterRecord> QueryTestMasters(string? productId = null, string? startDate = null,
        string? endDate = null, string? operatorName = null)
    {
        var sql = "SELECT * FROM testmaster WHERE 1=1";
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(productId))
        {
            sql += " AND productid LIKE @pid";
            parameters["@pid"] = $"%{productId}%";
        }
        if (!string.IsNullOrEmpty(startDate))
        {
            sql += " AND testdate >= @sd";
            parameters["@sd"] = startDate;
        }
        if (!string.IsNullOrEmpty(endDate))
        {
            sql += " AND testdate <= @ed";
            parameters["@ed"] = endDate;
        }
        if (!string.IsNullOrEmpty(operatorName))
        {
            sql += " AND operator = @op";
            parameters["@op"] = operatorName;
        }
        sql += " ORDER BY testdate DESC";

        return Query(sql, MapTestMaster, parameters);
    }

    public void UpdateTestMasterFlag(string productId, string testId, string flag)
    {
        var sql = "UPDATE testmaster SET flag = @flag WHERE productid = @pid AND testid = @tid;";
        Execute(sql, new() { ["@flag"] = flag, ["@pid"] = productId, ["@tid"] = testId });
    }

    public void UpdateTestMasterPostWeight(string productId, string testId, double postWeight,
        double lostWeight, double lostWeightPer, bool hasFlame, int flameStartTime, int flameDuration, string remark)
    {
        var sql = @"UPDATE testmaster SET
            postweight = @pw, lostweight = @lw, lostweight_per = @lwp,
            hasflame = @hf, flamestarttime = @fst, flameduration = @fd,
            remark = @remark
            WHERE productid = @pid AND testid = @tid;";
        Execute(sql, new()
        {
            ["@pw"] = postWeight, ["@lw"] = lostWeight, ["@lwp"] = lostWeightPer,
            ["@hf"] = hasFlame ? 1 : 0, ["@fst"] = flameStartTime, ["@fd"] = flameDuration,
            ["@remark"] = remark,
            ["@pid"] = productId, ["@tid"] = testId,
        });
    }

    public void UpdateTestMasterFinalTemps(string productId, string testId,
        double finalTF1, double finalTF2, double finalTS, double finalTC,
        double deltaTF1, double deltaTF2, double deltaTS, double deltaTC, double deltatf,
        int totalTestTime, int constPowerValue)
    {
        var sql = @"UPDATE testmaster SET
            finaltf1 = @ftf1, finaltf2 = @ftf2, finalts = @fts, finaltc = @ftc,
            deltatf1 = @dtf1, deltatf2 = @dtf2, deltats = @dts, deltatc = @dtc, deltatf = @dtf,
            totaltesttime = @ttt, constpowervalue = @cpv
            WHERE productid = @pid AND testid = @tid;";
        Execute(sql, new()
        {
            ["@ftf1"] = finalTF1, ["@ftf2"] = finalTF2, ["@fts"] = finalTS, ["@ftc"] = finalTC,
            ["@dtf1"] = deltaTF1, ["@dtf2"] = deltaTF2, ["@dts"] = deltaTS, ["@dtc"] = deltaTC, ["@dtf"] = deltatf,
            ["@ttt"] = totalTestTime, ["@cpv"] = constPowerValue,
            ["@pid"] = productId, ["@tid"] = testId,
        });
    }

    // ==================== 设备信息 ====================

    public Apparatus? GetFirstApparatus()
    {
        var sql = "SELECT * FROM apparatus LIMIT 1;";
        return QuerySingle(sql, r => new Apparatus
        {
            ApparatusId = r.GetString(r.GetOrdinal("apparatusid")),
            ApparatusName = r.GetString(r.GetOrdinal("apparatusname")),
            ComPort = r.IsDBNull(r.GetOrdinal("comport")) ? "" : r.GetString(r.GetOrdinal("comport")),
            BaudRate = r.GetInt32(r.GetOrdinal("baudrate")),
            ConstPower = r.GetInt32(r.GetOrdinal("constpower")),
            CalibrationDate = r.IsDBNull(r.GetOrdinal("calibrationdate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("calibrationdate"))),
            NextCalibrationDate = r.IsDBNull(r.GetOrdinal("nextcalibrationdate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("nextcalibrationdate"))),
        });
    }

    // ==================== 校准记录 ====================

    public void InsertCalibrationRecord(CalibrationRecord record)
    {
        var sql = @"INSERT INTO calibrationrecords (calibrationdate, operator, apparatusid, referencetemp, measuredtemp, deviation, remark)
                    VALUES (@cd, @op, @aid, @rt, @mt, @dev, @remark);";
        Execute(sql, new()
        {
            ["@cd"] = record.CalibrationDate, ["@op"] = record.Operator, ["@aid"] = record.ApparatusId,
            ["@rt"] = record.ReferenceTemp, ["@mt"] = record.MeasuredTemp, ["@dev"] = record.Deviation,
            ["@remark"] = record.Remark,
        });
    }

    public List<CalibrationRecord> GetCalibrationRecords()
    {
        var sql = "SELECT * FROM calibrationrecords ORDER BY id DESC;";
        return Query(sql, r => new CalibrationRecord
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            CalibrationDate = r.GetString(r.GetOrdinal("calibrationdate")),
            Operator = r.GetString(r.GetOrdinal("operator")),
            ApparatusId = r.GetString(r.GetOrdinal("apparatusid")),
            ReferenceTemp = r.GetDouble(r.GetOrdinal("referencetemp")),
            MeasuredTemp = r.GetDouble(r.GetOrdinal("measuredtemp")),
            Deviation = r.GetDouble(r.GetOrdinal("deviation")),
            Remark = r.IsDBNull(r.GetOrdinal("remark")) ? "" : r.GetString(r.GetOrdinal("remark")),
        });
    }

    // ==================== 样品信息 ====================

    public void InsertOrUpdateProduct(ProductMaster product)
    {
        var sql = @"INSERT OR REPLACE INTO productmaster (productid, productname, specification, height, diameter, createtime)
                    VALUES (@pid, @pn, @spec, @h, @d, @ct);";
        Execute(sql, new()
        {
            ["@pid"] = product.ProductId, ["@pn"] = product.ProductName,
            ["@spec"] = product.Specification, ["@h"] = product.Height, ["@d"] = product.Diameter,
            ["@ct"] = product.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
        });
    }

    public List<Operator> GetOperatorsByRole(string role)
    {
        var sql = "SELECT * FROM operators WHERE role = @role";
        return Query(sql, r => new Operator
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            UserId = r.GetString(r.GetOrdinal("userid")),
            Username = r.GetString(r.GetOrdinal("username")),
            Pwd = r.GetString(r.GetOrdinal("pwd")),
            Role = r.GetString(r.GetOrdinal("role")),
        }, new() { ["@role"] = role });
    }

    private TestMasterRecord MapTestMaster(SqliteDataReader r)
    {
        return new TestMasterRecord
        {
            ProductId = r.GetString(r.GetOrdinal("productid")),
            TestId = r.GetString(r.GetOrdinal("testid")),
            TestDate = r.IsDBNull(r.GetOrdinal("testdate")) ? "" : r.GetString(r.GetOrdinal("testdate")),
            Operator = r.IsDBNull(r.GetOrdinal("operator")) ? "" : r.GetString(r.GetOrdinal("operator")),
            ApparatusId = r.IsDBNull(r.GetOrdinal("apparatusid")) ? "" : r.GetString(r.GetOrdinal("apparatusid")),
            ApparatusName = r.IsDBNull(r.GetOrdinal("apparatusname")) ? "" : r.GetString(r.GetOrdinal("apparatusname")),
            AmbientTemp = r.IsDBNull(r.GetOrdinal("ambienttemp")) ? 0 : r.GetDouble(r.GetOrdinal("ambienttemp")),
            AmbientHumidity = r.IsDBNull(r.GetOrdinal("ambienthumidity")) ? 0 : r.GetDouble(r.GetOrdinal("ambienthumidity")),
            ProductName = r.IsDBNull(r.GetOrdinal("productname")) ? "" : r.GetString(r.GetOrdinal("productname")),
            Specification = r.IsDBNull(r.GetOrdinal("specification")) ? "" : r.GetString(r.GetOrdinal("specification")),
            Height = r.IsDBNull(r.GetOrdinal("height")) ? 0 : r.GetDouble(r.GetOrdinal("height")),
            Diameter = r.IsDBNull(r.GetOrdinal("diameter")) ? 0 : r.GetDouble(r.GetOrdinal("diameter")),
            PreWeight = r.IsDBNull(r.GetOrdinal("preweight")) ? 0 : r.GetDouble(r.GetOrdinal("preweight")),
            PostWeight = r.IsDBNull(r.GetOrdinal("postweight")) ? 0 : r.GetDouble(r.GetOrdinal("postweight")),
            LostWeight = r.IsDBNull(r.GetOrdinal("lostweight")) ? 0 : r.GetDouble(r.GetOrdinal("lostweight")),
            LostWeightPer = r.IsDBNull(r.GetOrdinal("lostweight_per")) ? 0 : r.GetDouble(r.GetOrdinal("lostweight_per")),
            FinalTF1 = r.IsDBNull(r.GetOrdinal("finaltf1")) ? 0 : r.GetDouble(r.GetOrdinal("finaltf1")),
            FinalTF2 = r.IsDBNull(r.GetOrdinal("finaltf2")) ? 0 : r.GetDouble(r.GetOrdinal("finaltf2")),
            FinalTS = r.IsDBNull(r.GetOrdinal("finalts")) ? 0 : r.GetDouble(r.GetOrdinal("finalts")),
            FinalTC = r.IsDBNull(r.GetOrdinal("finaltc")) ? 0 : r.GetDouble(r.GetOrdinal("finaltc")),
            Deltatf = r.IsDBNull(r.GetOrdinal("deltatf")) ? 0 : r.GetDouble(r.GetOrdinal("deltatf")),
            DeltaTF1 = r.IsDBNull(r.GetOrdinal("deltatf1")) ? 0 : r.GetDouble(r.GetOrdinal("deltatf1")),
            DeltaTF2 = r.IsDBNull(r.GetOrdinal("deltatf2")) ? 0 : r.GetDouble(r.GetOrdinal("deltatf2")),
            DeltaTS = r.IsDBNull(r.GetOrdinal("deltats")) ? 0 : r.GetDouble(r.GetOrdinal("deltats")),
            DeltaTC = r.IsDBNull(r.GetOrdinal("deltatc")) ? 0 : r.GetDouble(r.GetOrdinal("deltatc")),
            HasFlame = r.GetInt32(r.GetOrdinal("hasflame")) != 0,
            FlameStartTime = r.GetInt32(r.GetOrdinal("flamestarttime")),
            FlameDuration = r.GetInt32(r.GetOrdinal("flameduration")),
            TestMode = r.IsDBNull(r.GetOrdinal("testmode")) ? "Standard60Min" : r.GetString(r.GetOrdinal("testmode")),
            TotalTestTime = r.GetInt32(r.GetOrdinal("totaltesttime")),
            TargetDuration = r.IsDBNull(r.GetOrdinal("targetduration")) ? 3600 : r.GetInt32(r.GetOrdinal("targetduration")),
            ConstPowerValue = r.IsDBNull(r.GetOrdinal("constpowervalue")) ? 0 : r.GetInt32(r.GetOrdinal("constpowervalue")),
            Flag = r.IsDBNull(r.GetOrdinal("flag")) ? "00000000" : r.GetString(r.GetOrdinal("flag")),
            Remark = r.IsDBNull(r.GetOrdinal("remark")) ? "" : r.GetString(r.GetOrdinal("remark")),
            CalibrationDate = r.IsDBNull(r.GetOrdinal("calibrationdate")) ? DateTime.MinValue : DateTime.Parse(r.GetString(r.GetOrdinal("calibrationdate"))),
            DataFilePath = r.IsDBNull(r.GetOrdinal("datafilepath")) ? "" : r.GetString(r.GetOrdinal("datafilepath")),
        };
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
    }
}

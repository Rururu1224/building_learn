-- ============================================================
-- ISO11820 建材不燃性试验仿真系统 — 建库脚本
-- 说明：程序启动检测数据库不存在时自动执行此脚本
-- ============================================================

-- 1. operators 表（操作员/用户账号）
-- 注意：此表没有主键约束，密码明文存储
CREATE TABLE IF NOT EXISTS "operators" (
    "userid"    TEXT NOT NULL,   -- 用户ID
    "username"  TEXT NOT NULL,   -- 登录用户名
    "pwd"       TEXT NOT NULL,   -- 明文密码
    "usertype"  TEXT NOT NULL    -- 角色：admin 或 operator
);

-- 2. apparatus 表（试验设备信息）
CREATE TABLE IF NOT EXISTS "apparatus" (
    "apparatusid"   INTEGER NOT NULL CONSTRAINT "PK_apparatus" PRIMARY KEY,
    "innernumber"   TEXT NOT NULL,       -- 设备内部编号
    "apparatusname" TEXT NOT NULL,       -- 设备名称
    "checkdatef"    date NOT NULL,       -- 检定有效期开始
    "checkdatet"    date NOT NULL,       -- 检定有效期结束
    "pidport"       TEXT NOT NULL,       -- PID串口
    "powerport"     TEXT NOT NULL,       -- 功率串口
    "constpower"    INTEGER NULL         -- 上次记录的恒功率值
);

-- 3. productmaster 表（样品信息）
CREATE TABLE IF NOT EXISTS "productmaster" (
    "productid"   TEXT NOT NULL CONSTRAINT "PK_productmaster" PRIMARY KEY,
    "productname" TEXT NOT NULL,   -- 样品名称
    "specific"    TEXT NOT NULL,   -- 规格型号
    "diameter"    REAL NOT NULL,   -- 直径（mm）
    "height"      REAL NOT NULL,   -- 高度（mm）
    "flag"        TEXT NULL        -- 备用字段
);

-- 4. testmaster 表（试验记录 — 核心表）
-- 联合主键：(productid, testid)
CREATE TABLE IF NOT EXISTS "testmaster" (
    "productid"        TEXT NOT NULL,           -- 样品编号（联合主键 + 外键）
    "testid"           TEXT NOT NULL,           -- 试验ID
    "testdate"         date NOT NULL,           -- 试验日期
    "ambtemp"          REAL NOT NULL,           -- 环境温度（°C）
    "ambhumi"          REAL NOT NULL,           -- 环境湿度（%）
    "according"        TEXT NOT NULL,           -- 试验依据
    "operator"         TEXT NOT NULL,           -- 操作员用户名
    "apparatusid"      TEXT NOT NULL,           -- 设备编号
    "apparatusname"    TEXT NOT NULL,           -- 设备名称
    "apparatuschkdate" date NOT NULL,           -- 设备检定日期
    "rptno"            TEXT NOT NULL,           -- 报告编号
    "preweight"        REAL NOT NULL,           -- 试验前质量（g）
    "postweight"       REAL NOT NULL,           -- 试验后质量（g）
    "lostweight"       REAL NOT NULL,           -- 失重量
    "lostweight_per"   REAL NOT NULL,           -- 失重率（%）
    "totaltesttime"    INTEGER NOT NULL,        -- 总试验时长（秒）
    "constpower"       INTEGER NOT NULL,        -- 恒功率值
    "phenocode"        TEXT NOT NULL,           -- 现象编码
    "flametime"        INTEGER NOT NULL,        -- 火焰开始时刻
    "flameduration"    INTEGER NOT NULL,        -- 火焰持续时间
    "maxtf1"           REAL NOT NULL,           -- 炉温1最大值
    "maxtf2"           REAL NOT NULL,           -- 炉温2最大值
    "maxts"            REAL NOT NULL,           -- 表面温最大值
    "maxtc"            REAL NOT NULL,           -- 中心温最大值
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
    "deltatf"          REAL NOT NULL,           -- 样品温升（判定项）
    "deltats"          REAL NOT NULL,
    "deltatc"          REAL NOT NULL,
    "memo"             TEXT NULL,
    "flag"             TEXT NULL,

    CONSTRAINT "PK_testmaster" PRIMARY KEY ("productid", "testid"),
    CONSTRAINT "FK_testmaster_productmaster" FOREIGN KEY ("productid") REFERENCES "productmaster" ("productid")
);

-- testmaster 索引
CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate"            ON "testmaster" ("testdate");
CREATE INDEX IF NOT EXISTS "IX_Testmaster_Operator"            ON "testmaster" ("operator");
CREATE INDEX IF NOT EXISTS "IX_Testmaster_Testdate_Productid"  ON "testmaster" ("testdate", "productid");

-- 5. sensors 表（传感器通道配置）
CREATE TABLE IF NOT EXISTS "sensors" (
    "sensorid"    INTEGER NOT NULL CONSTRAINT "PK_sensors" PRIMARY KEY,
    "sensorname"  TEXT NOT NULL,   -- 传感器代号
    "dispname"    TEXT NOT NULL,   -- 显示名
    "sensorgroup" TEXT NOT NULL,   -- 分组标识
    "unit"        TEXT NOT NULL,   -- 单位
    "discription" TEXT NOT NULL,   -- 描述
    "flag"        TEXT NOT NULL,   -- 标记
    "signalzero"  REAL NOT NULL,
    "signalspan"  REAL NOT NULL,
    "outputzero"  REAL NOT NULL,
    "outputspan"  REAL NOT NULL,
    "outputvalue" REAL NOT NULL,
    "inputvalue"  REAL NOT NULL,
    "signaltype"  INTEGER NOT NULL -- 信号类型
);

-- 6. CalibrationRecords 表（设备校准历史）— 注意大写开头
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
    "TAvgAxis1"   REAL NULL,   "TAvgAxis2" REAL NULL,   "TAvgAxis3" REAL NULL,
    "TAvgLevela"  REAL NULL,   "TAvgLevelb" REAL NULL,  "TAvgLevelc" REAL NULL,
    "TDevAxis1"   REAL NULL,   "TDevAxis2" REAL NULL,   "TDevAxis3" REAL NULL,
    "TDevLevela"  REAL NULL,   "TDevLevelb" REAL NULL,  "TDevLevelc" REAL NULL,
    "TAvgDevAxis" REAL NULL,   "TAvgDevLevel" REAL NULL,
    "CenterTempData" TEXT NULL,
    "Memo"           TEXT NULL
);

CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Date"     ON "CalibrationRecords" ("CalibrationDate");
CREATE INDEX IF NOT EXISTS "IX_CalibrationRecord_Operator" ON "CalibrationRecords" ("Operator");

-- ============================================================
-- 初始数据
-- ============================================================

-- 操作员初始化
INSERT INTO operators (userid, username, pwd, usertype)
SELECT '1', 'admin', '123456', 'admin'
WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');

INSERT INTO operators (userid, username, pwd, usertype)
SELECT '2', 'experimenter', '123456', 'operator'
WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');

-- 设备初始化
INSERT INTO apparatus (apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower)
SELECT 0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0);

-- 传感器初始化（5个主通道，4~15为备用通道）
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

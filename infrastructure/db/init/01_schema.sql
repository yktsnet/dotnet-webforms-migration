CREATE TABLE IF NOT EXISTS employees (
    id                 VARCHAR(20)   PRIMARY KEY,
    name               VARCHAR(100)  NOT NULL,
    hourly_wage        NUMERIC(10,2) NOT NULL,
    round_unit_minutes INT           NOT NULL DEFAULT 1,
    created_at         TIMESTAMP     DEFAULT NOW()
);
CREATE TABLE IF NOT EXISTS attendance_logs (
    id             SERIAL      PRIMARY KEY,
    employee_id    VARCHAR(20) NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    clock_in       TIMESTAMP,
    clock_out      TIMESTAMP,
    break_minutes  INT         NOT NULL DEFAULT 60,
    is_corrected   BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at     TIMESTAMP   DEFAULT NOW()
);
CREATE TABLE IF NOT EXISTS admin_users (
    id            SERIAL       PRIMARY KEY,
    username      VARCHAR(50)  UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at    TIMESTAMP    DEFAULT NOW()
);
CREATE TABLE IF NOT EXISTS employee_profiles (
    employee_id       VARCHAR(20) PRIMARY KEY REFERENCES employees(id) ON DELETE CASCADE,
    avg_clockout_time TIME        NOT NULL DEFAULT '18:00:00',
    updated_at        TIMESTAMP   DEFAULT NOW()
);
-- シードデータ
INSERT INTO employees (id, name, hourly_wage, round_unit_minutes) VALUES
    ('EMP-001', '山田 太郎', 1200, 15),
    ('EMP-002', '鈴木 花子', 1100, 30),
    ('EMP-003', '田中 一郎', 1050,  1)
ON CONFLICT DO NOTHING;
-- admin / admin123
INSERT INTO admin_users (username, password_hash) VALUES
    ('admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh23')
ON CONFLICT DO NOTHING;

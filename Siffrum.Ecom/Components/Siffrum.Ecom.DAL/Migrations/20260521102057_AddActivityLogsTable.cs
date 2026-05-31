using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddActivityLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- ═══════════════════════════════════════════════════════════════════
                -- ACTIVITY LOGS TABLE
                -- Tracks all admin and seller actions with IP and device information
                -- ═══════════════════════════════════════════════════════════════════
                CREATE TABLE IF NOT EXISTS activity_logs (
                    id                  bigserial       PRIMARY KEY,
                    user_id             bigint          NOT NULL,
                    user_type           varchar(20)     NOT NULL,
                    user_name           varchar(200)    NOT NULL,
                    user_email          varchar(200)    NULL,
                    action_type         varchar(50)     NOT NULL,
                    action_category     varchar(50)     NOT NULL,
                    entity_type         varchar(50)     NULL,
                    entity_id           bigint          NULL,
                    entity_name         varchar(200)    NULL,
                    description         varchar(1000)   NOT NULL,
                    old_values          text            NULL,
                    new_values          text            NULL,
                    ip_address          varchar(50)     NULL,
                    mac_address         varchar(50)     NULL,
                    user_agent          varchar(500)    NULL,
                    device_info         varchar(200)    NULL,
                    platform            varchar(20)     NULL,
                    success             boolean         NOT NULL DEFAULT true,
                    error_message       varchar(1000)   NULL,
                    created_at          timestamp       NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
                    created_by          varchar(100)    NULL,
                    updated_at          timestamp       NULL,
                    updated_by          varchar(100)    NULL,
                    deleted_at          timestamp       NULL,
                    deleted_by          varchar(100)    NULL
                );

                -- ═══════════════════════════════════════════════════════════════════
                -- INDEXES for efficient querying
                -- ═══════════════════════════════════════════════════════════════════
                CREATE INDEX IF NOT EXISTS ix_activity_logs_user_id ON activity_logs(user_id);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_user_type ON activity_logs(user_type);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_action_type ON activity_logs(action_type);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_created_at ON activity_logs(created_at DESC);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_user_id_created_at ON activity_logs(user_id, created_at DESC);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_action_category ON activity_logs(action_category);
                CREATE INDEX IF NOT EXISTS ix_activity_logs_entity_type_entity_id ON activity_logs(entity_type, entity_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS activity_logs CASCADE;
            ");
        }
    }
}

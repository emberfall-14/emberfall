﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class UnifiedModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unban");

            migrationBuilder.DropTable(
                name: "ban");

            migrationBuilder.DropColumn(
                name: "selected_character_slot",
                table: "preference");

            migrationBuilder.CreateTable(
                name: "PreferenceProfile",
                columns: table => new
                {
                    PreferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenceProfile", x => new { x.PreferenceId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_PreferenceProfile_preference_PreferenceId",
                        column: x => x.PreferenceId,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreferenceProfile_profile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_ban",
                columns: table => new
                {
                    server_ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban", x => x.server_ban_id);
                    table.CheckConstraint("HaveEitherAddressOrUserId", "address IS NOT NULL OR user_id IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "server_unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_server_unban_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_ban",
                        principalColumn: "server_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_user_id",
                table: "player",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_connection_log_user_id",
                table: "connection_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceProfile_PreferenceId",
                table: "PreferenceProfile",
                column: "PreferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceProfile_ProfileId",
                table: "PreferenceProfile",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_address",
                table: "server_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_user_id",
                table: "server_ban",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_unban_ban_id",
                table: "server_unban",
                column: "ban_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenceProfile");

            migrationBuilder.DropTable(
                name: "server_unban");

            migrationBuilder.DropTable(
                name: "server_ban");

            migrationBuilder.DropIndex(
                name: "IX_player_user_id",
                table: "player");

            migrationBuilder.DropIndex(
                name: "IX_connection_log_user_id",
                table: "connection_log");

            migrationBuilder.AddColumn<int>(
                name: "selected_character_slot",
                table: "preference",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ban",
                columns: table => new
                {
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban", x => x.ban_id);
                });

            migrationBuilder.CreateTable(
                name: "unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_unban_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unban_ban_id",
                table: "unban",
                column: "ban_id",
                unique: true);
        }
    }
}

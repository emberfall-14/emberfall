﻿// <auto-generated />
using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    [DbContext(typeof(SqliteServerDbContext))]
    partial class SqliteServerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0");

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<int?>("AdminRankId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT")
                        .HasColumnName("title");

                    b.HasKey("UserId")
                        .HasName("PK_admin");

                    b.HasIndex("AdminRankId")
                        .HasDatabaseName("IX_admin_admin_rank_id");

                    b.ToTable("admin", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminFlag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_flag_id");

                    b.Property<Guid>("AdminId")
                        .HasColumnType("TEXT")
                        .HasColumnName("admin_id");

                    b.Property<string>("Flag")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("flag");

                    b.Property<bool>("Negative")
                        .HasColumnType("INTEGER")
                        .HasColumnName("negative");

                    b.HasKey("Id")
                        .HasName("PK_admin_flag");

                    b.HasIndex("AdminId")
                        .HasDatabaseName("IX_admin_flag_admin_id");

                    b.HasIndex("Flag", "AdminId")
                        .IsUnique();

                    b.ToTable("admin_flag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_log_id");

                    b.Property<int>("RoundId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("round_id");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT")
                        .HasColumnName("date");

                    b.Property<sbyte>("Impact")
                        .HasColumnType("INTEGER")
                        .HasColumnName("impact");

                    b.Property<string>("Json")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("json");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("message");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER")
                        .HasColumnName("type");

                    b.HasKey("Id", "RoundId")
                        .HasName("PK_admin_log");

                    b.HasIndex("RoundId")
                        .HasDatabaseName("IX_admin_log_round_id");

                    b.HasIndex("Type")
                        .HasDatabaseName("IX_admin_log_type");

                    b.ToTable("admin_log", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminLogEntity", b =>
                {
                    b.Property<int>("Uid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("uid");

                    b.Property<int?>("AdminLogId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_log_id");

                    b.Property<int?>("AdminLogRoundId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_log_round_id");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.HasKey("Uid")
                        .HasName("PK_admin_log_entity");

                    b.HasIndex("AdminLogId", "AdminLogRoundId")
                        .HasDatabaseName("IX_admin_log_entity_admin_log_id_admin_log_round_id");

                    b.ToTable("admin_log_entity", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminLogPlayer", b =>
                {
                    b.Property<Guid>("PlayerUserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("player_user_id");

                    b.Property<int>("LogId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("log_id");

                    b.Property<int>("RoundId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("round_id");

                    b.HasKey("PlayerUserId", "LogId", "RoundId")
                        .HasName("PK_admin_log_player");

                    b.HasIndex("LogId", "RoundId");

                    b.ToTable("admin_log_player", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminRank", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("PK_admin_rank");

                    b.ToTable("admin_rank", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AdminRankFlag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_flag_id");

                    b.Property<int>("AdminRankId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("admin_rank_id");

                    b.Property<string>("Flag")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("flag");

                    b.HasKey("Id")
                        .HasName("PK_admin_rank_flag");

                    b.HasIndex("AdminRankId")
                        .HasDatabaseName("IX_admin_rank_flag_admin_rank_id");

                    b.HasIndex("Flag", "AdminRankId")
                        .IsUnique();

                    b.ToTable("admin_rank_flag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Antag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("antag_id");

                    b.Property<string>("AntagName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("antag_name");

                    b.Property<int>("ProfileId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.HasKey("Id")
                        .HasName("PK_antag");

                    b.HasIndex("ProfileId", "AntagName")
                        .IsUnique();

                    b.ToTable("antag", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.AssignedUserId", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("assigned_user_id_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("PK_assigned_user_id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.HasIndex("UserName")
                        .IsUnique();

                    b.ToTable("assigned_user_id", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Job", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("job_id");

                    b.Property<string>("JobName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("job_name");

                    b.Property<int>("Priority")
                        .HasColumnType("INTEGER")
                        .HasColumnName("priority");

                    b.Property<int>("ProfileId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.HasKey("Id")
                        .HasName("PK_job");

                    b.HasIndex("ProfileId")
                        .HasDatabaseName("IX_job_profile_id");

                    b.HasIndex("ProfileId", "JobName")
                        .IsUnique();

                    b.HasIndex(new[] { "ProfileId" }, "IX_job_one_high_priority")
                        .IsUnique()
                        .HasFilter("priority = 3");

                    b.ToTable("job", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("player_id");

                    b.Property<DateTime>("FirstSeenTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("first_seen_time");

                    b.Property<string>("LastSeenAddress")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_address");

                    b.Property<byte[]>("LastSeenHWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("last_seen_hwid");

                    b.Property<DateTime>("LastSeenTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_time");

                    b.Property<string>("LastSeenUserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("last_seen_user_name");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_player");

                    b.HasAlternateKey("UserId")
                        .HasName("ak_player_user_id");

                    b.HasIndex("LastSeenUserName");

                    b.ToTable("player", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Preference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("preference_id");

                    b.Property<string>("AdminOOCColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("admin_ooc_color");

                    b.Property<int>("SelectedCharacterSlot")
                        .HasColumnType("INTEGER")
                        .HasColumnName("selected_character_slot");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_preference");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("preference", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("profile_id");

                    b.Property<int>("Age")
                        .HasColumnType("INTEGER")
                        .HasColumnName("age");

                    b.Property<string>("Backpack")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("backpack");

                    b.Property<string>("CharacterName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("char_name");

                    b.Property<string>("Clothing")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("clothing");

                    b.Property<string>("EyeColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("eye_color");

                    b.Property<string>("FacialHairColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("facial_hair_color");

                    b.Property<string>("FacialHairName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("facial_hair_name");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("gender");

                    b.Property<string>("HairColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("hair_color");

                    b.Property<string>("HairName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("hair_name");

                    b.Property<int>("PreferenceId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("preference_id");

                    b.Property<int>("PreferenceUnavailable")
                        .HasColumnType("INTEGER")
                        .HasColumnName("pref_unavailable");

                    b.Property<string>("Sex")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("sex");

                    b.Property<string>("SkinColor")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("skin_color");

                    b.Property<int>("Slot")
                        .HasColumnType("INTEGER")
                        .HasColumnName("slot");

                    b.Property<string>("Species")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("species");

                    b.HasKey("Id")
                        .HasName("PK_profile");

                    b.HasIndex("PreferenceId")
                        .HasDatabaseName("IX_profile_preference_id");

                    b.HasIndex("Slot", "PreferenceId")
                        .IsUnique();

                    b.ToTable("profile", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Round", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("round_id");

                    b.HasKey("Id")
                        .HasName("PK_round");

                    b.ToTable("round", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteConnectionLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("connection_log_id");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("address");

                    b.Property<byte[]>("HWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("hwid");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT")
                        .HasColumnName("time");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("PK_connection_log");

                    b.ToTable("connection_log", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("ban_id");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT")
                        .HasColumnName("address");

                    b.Property<DateTime>("BanTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("ban_time");

                    b.Property<Guid?>("BanningAdmin")
                        .HasColumnType("TEXT")
                        .HasColumnName("banning_admin");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("expiration_time");

                    b.Property<byte[]>("HWId")
                        .HasColumnType("BLOB")
                        .HasColumnName("hwid");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("reason");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("TEXT")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("PK_ban");

                    b.ToTable("ban", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerUnban", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("unban_id");

                    b.Property<int>("BanId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("ban_id");

                    b.Property<DateTime>("UnbanTime")
                        .HasColumnType("TEXT")
                        .HasColumnName("unban_time");

                    b.Property<Guid?>("UnbanningAdmin")
                        .HasColumnType("TEXT")
                        .HasColumnName("unbanning_admin");

                    b.HasKey("Id")
                        .HasName("PK_unban");

                    b.HasIndex("BanId")
                        .IsUnique();

                    b.ToTable("unban", (string)null);
                });

            modelBuilder.Entity("PlayerRound", b =>
                {
                    b.Property<int>("PlayersId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("players_id");

                    b.Property<int>("RoundsId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("rounds_id");

                    b.HasKey("PlayersId", "RoundsId")
                        .HasName("PK_player_round");

                    b.HasIndex("RoundsId")
                        .HasDatabaseName("IX_player_round_rounds_id");

                    b.ToTable("player_round", (string)null);
                });

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.HasOne("Content.Server.Database.AdminRank", "AdminRank")
                        .WithMany("Admins")
                        .HasForeignKey("AdminRankId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .HasConstraintName("FK_admin_admin_rank_admin_rank_id");

                    b.Navigation("AdminRank");
                });

            modelBuilder.Entity("Content.Server.Database.AdminFlag", b =>
                {
                    b.HasOne("Content.Server.Database.Admin", "Admin")
                        .WithMany("Flags")
                        .HasForeignKey("AdminId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_flag_admin_admin_id");

                    b.Navigation("Admin");
                });

            modelBuilder.Entity("Content.Server.Database.AdminLog", b =>
                {
                    b.HasOne("Content.Server.Database.Round", "Round")
                        .WithMany("AdminLogs")
                        .HasForeignKey("RoundId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_log_round_round_id");

                    b.Navigation("Round");
                });

            modelBuilder.Entity("Content.Server.Database.AdminLogEntity", b =>
                {
                    b.HasOne("Content.Server.Database.AdminLog", null)
                        .WithMany("Entities")
                        .HasForeignKey("AdminLogId", "AdminLogRoundId")
                        .HasConstraintName("FK_admin_log_entity_admin_log_admin_log_id_admin_log_round_id");
                });

            modelBuilder.Entity("Content.Server.Database.AdminLogPlayer", b =>
                {
                    b.HasOne("Content.Server.Database.Player", "Player")
                        .WithMany("AdminLogs")
                        .HasForeignKey("PlayerUserId")
                        .HasPrincipalKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_log_player_player_player_user_id");

                    b.HasOne("Content.Server.Database.AdminLog", "Log")
                        .WithMany("Players")
                        .HasForeignKey("LogId", "RoundId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_log_player_admin_log_log_id_round_id");

                    b.Navigation("Log");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("Content.Server.Database.AdminRankFlag", b =>
                {
                    b.HasOne("Content.Server.Database.AdminRank", "Rank")
                        .WithMany("Flags")
                        .HasForeignKey("AdminRankId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_admin_rank_flag_admin_rank_admin_rank_id");

                    b.Navigation("Rank");
                });

            modelBuilder.Entity("Content.Server.Database.Antag", b =>
                {
                    b.HasOne("Content.Server.Database.Profile", "Profile")
                        .WithMany("Antags")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_antag_profile_profile_id");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("Content.Server.Database.Job", b =>
                {
                    b.HasOne("Content.Server.Database.Profile", "Profile")
                        .WithMany("Jobs")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_job_profile_profile_id");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.HasOne("Content.Server.Database.Preference", "Preference")
                        .WithMany("Profiles")
                        .HasForeignKey("PreferenceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_profile_preference_preference_id");

                    b.Navigation("Preference");
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerUnban", b =>
                {
                    b.HasOne("Content.Server.Database.SqliteServerBan", "Ban")
                        .WithOne("Unban")
                        .HasForeignKey("Content.Server.Database.SqliteServerUnban", "BanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_unban_ban_ban_id");

                    b.Navigation("Ban");
                });

            modelBuilder.Entity("PlayerRound", b =>
                {
                    b.HasOne("Content.Server.Database.Player", null)
                        .WithMany()
                        .HasForeignKey("PlayersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_player_round_player_players_id");

                    b.HasOne("Content.Server.Database.Round", null)
                        .WithMany()
                        .HasForeignKey("RoundsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_player_round_round_rounds_id");
                });

            modelBuilder.Entity("Content.Server.Database.Admin", b =>
                {
                    b.Navigation("Flags");
                });

            modelBuilder.Entity("Content.Server.Database.AdminLog", b =>
                {
                    b.Navigation("Entities");

                    b.Navigation("Players");
                });

            modelBuilder.Entity("Content.Server.Database.AdminRank", b =>
                {
                    b.Navigation("Admins");

                    b.Navigation("Flags");
                });

            modelBuilder.Entity("Content.Server.Database.Player", b =>
                {
                    b.Navigation("AdminLogs");
                });

            modelBuilder.Entity("Content.Server.Database.Preference", b =>
                {
                    b.Navigation("Profiles");
                });

            modelBuilder.Entity("Content.Server.Database.Profile", b =>
                {
                    b.Navigation("Antags");

                    b.Navigation("Jobs");
                });

            modelBuilder.Entity("Content.Server.Database.Round", b =>
                {
                    b.Navigation("AdminLogs");
                });

            modelBuilder.Entity("Content.Server.Database.SqliteServerBan", b =>
                {
                    b.Navigation("Unban");
                });
#pragma warning restore 612, 618
        }
    }
}

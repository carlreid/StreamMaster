﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StreamMaster.Infrastructure.EF.PGSQL;

#nullable disable

namespace StreamMaster.Infrastructure.EF.PGSQL.Migrations.Repository
{
    [DbContext(typeof(PGSQLRepositoryContext))]
    partial class PGSQLRepositoryContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "citext");
            NpgsqlModelBuilderExtensions.UseIdentityAlwaysColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("FriendlyName")
                        .HasColumnType("text");

                    b.Property<string>("Xml")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("DataProtectionKeys");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.APIKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DeviceName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("Expiration")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("LastUsedAt")
                        .HasColumnType("timestamp with time zone");

                    b.PrimitiveCollection<List<string>>("Scopes")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("APIKeys");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.ChannelGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<int>("ActiveCount")
                        .HasColumnType("integer");

                    b.Property<int>("HiddenCount")
                        .HasColumnType("integer");

                    b.Property<bool>("IsHidden")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsReadOnly")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("RegexMatch")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("TotalCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("ChannelGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.Device", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ApiKeyId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("DeviceId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("DeviceType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IPAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("LastActivity")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserAgent")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.EPGFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<bool>("AutoUpdate")
                        .HasColumnType("boolean");

                    b.Property<int>("ChannelCount")
                        .HasColumnType("integer");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("DownloadErrors")
                        .HasColumnType("integer");

                    b.Property<int>("EPGNumber")
                        .HasColumnType("integer");

                    b.Property<bool>("FileExists")
                        .HasColumnType("boolean");

                    b.Property<string>("FileExtension")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("HoursToUpdate")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastDownloadAttempt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastDownloaded")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MinimumMinutesBetweenDownloads")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("ProgrammeCount")
                        .HasColumnType("integer");

                    b.Property<int>("SMFileType")
                        .HasColumnType("integer");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("TimeShift")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("EPGFiles");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.M3UFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<bool>("AutoSetChannelNumbers")
                        .HasColumnType("boolean");

                    b.Property<bool>("AutoUpdate")
                        .HasColumnType("boolean");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("DefaultStreamGroupName")
                        .HasColumnType("text");

                    b.Property<int>("DownloadErrors")
                        .HasColumnType("integer");

                    b.Property<bool>("FileExists")
                        .HasColumnType("boolean");

                    b.Property<string>("FileExtension")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("HoursToUpdate")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastDownloadAttempt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastDownloaded")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("M3U8OutPutProfile")
                        .HasColumnType("text");

                    b.Property<int>("M3UKey")
                        .HasColumnType("integer");

                    b.Property<int>("M3UName")
                        .HasColumnType("integer");

                    b.Property<int>("MaxStreamCount")
                        .HasColumnType("integer");

                    b.Property<int>("MinimumMinutesBetweenDownloads")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("SMFileType")
                        .HasColumnType("integer");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("StartingChannelNumber")
                        .HasColumnType("integer");

                    b.Property<int>("StreamCount")
                        .HasColumnType("integer");

                    b.Property<bool>("SyncChannels")
                        .HasColumnType("boolean");

                    b.Property<string>("Url")
                        .HasColumnType("citext");

                    b.PrimitiveCollection<List<string>>("VODTags")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.HasKey("Id");

                    b.ToTable("M3UFiles");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.M3UGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsIncluded")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsPPV")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsUser")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsVOD")
                        .HasColumnType("boolean");

                    b.Property<int>("M3UFileId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("TotalCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("M3UGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("BaseStreamID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ChannelId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("ChannelNumber")
                        .HasColumnType("integer");

                    b.Property<string>("ClientUserAgent")
                        .HasColumnType("citext");

                    b.Property<string>("CommandProfileName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("EPGId")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("Group")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("GroupTitle")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<bool>("IsHidden")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("boolean");

                    b.Property<string>("Logo")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("M3UFileId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("SMChannelType")
                        .HasColumnType("integer");

                    b.Property<string>("StationId")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("TVGName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("TimeShift")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("SMChannels");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannelChannelLink", b =>
                {
                    b.Property<int>("ParentSMChannelId")
                        .HasColumnType("integer");

                    b.Property<int>("SMChannelId")
                        .HasColumnType("integer");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.HasKey("ParentSMChannelId", "SMChannelId");

                    b.HasIndex("SMChannelId");

                    b.ToTable("SMChannelChannelLinks");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannelStreamLink", b =>
                {
                    b.Property<int>("SMChannelId")
                        .HasColumnType("integer");

                    b.Property<string>("SMStreamId")
                        .HasColumnType("text");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.HasKey("SMChannelId", "SMStreamId");

                    b.HasIndex("SMStreamId");

                    b.ToTable("SMChannelStreamLinks");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMStream", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("CUID")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("ChannelId")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("ChannelNumber")
                        .HasColumnType("integer");

                    b.Property<string>("ClientUserAgent")
                        .HasColumnType("citext");

                    b.Property<string>("CommandProfileName")
                        .HasColumnType("citext");

                    b.Property<string>("EPGID")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("ExtInf")
                        .HasColumnType("text");

                    b.Property<int>("FilePosition")
                        .HasColumnType("integer");

                    b.Property<string>("Group")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<bool>("IsHidden")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsUserCreated")
                        .HasColumnType("boolean");

                    b.Property<string>("Logo")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("M3UFileId")
                        .HasColumnType("integer");

                    b.Property<string>("M3UFileName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<bool>("NeedsDelete")
                        .HasColumnType("boolean");

                    b.Property<int>("SMStreamType")
                        .HasColumnType("integer");

                    b.Property<string>("StationId")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("TVGName")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("SMStreams");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<bool>("CreateSTRM")
                        .HasColumnType("boolean");

                    b.Property<string>("DeviceID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("GroupKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsReadOnly")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsSystem")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("ShowIntros")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("StreamGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupChannelGroup", b =>
                {
                    b.Property<int>("ChannelGroupId")
                        .HasColumnType("integer");

                    b.Property<int>("StreamGroupId")
                        .HasColumnType("integer");

                    b.HasKey("ChannelGroupId", "StreamGroupId");

                    b.HasIndex("StreamGroupId");

                    b.ToTable("StreamGroupChannelGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupProfile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("CommandProfileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OutputProfileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProfileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("StreamGroupId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("StreamGroupId");

                    b.ToTable("StreamGroupProfiles");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupSMChannelLink", b =>
                {
                    b.Property<int>("StreamGroupId")
                        .HasColumnType("integer");

                    b.Property<int>("SMChannelId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsReadOnly")
                        .HasColumnType("boolean");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.HasKey("StreamGroupId", "SMChannelId");

                    b.HasIndex("SMChannelId");

                    b.ToTable("StreamGroupSMChannelLink");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SystemKeyValue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.HasKey("Id");

                    b.ToTable("SystemKeyValues");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.UserGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("citext");

                    b.Property<int>("TotalCount")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("UserGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannelChannelLink", b =>
                {
                    b.HasOne("StreamMaster.Domain.Models.SMChannel", "ParentSMChannel")
                        .WithMany("SMChannels")
                        .HasForeignKey("ParentSMChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StreamMaster.Domain.Models.SMChannel", "SMChannel")
                        .WithMany()
                        .HasForeignKey("SMChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentSMChannel");

                    b.Navigation("SMChannel");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannelStreamLink", b =>
                {
                    b.HasOne("StreamMaster.Domain.Models.SMChannel", "SMChannel")
                        .WithMany("SMStreams")
                        .HasForeignKey("SMChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StreamMaster.Domain.Models.SMStream", "SMStream")
                        .WithMany()
                        .HasForeignKey("SMStreamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMChannel");

                    b.Navigation("SMStream");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupChannelGroup", b =>
                {
                    b.HasOne("StreamMaster.Domain.Models.ChannelGroup", "ChannelGroup")
                        .WithMany()
                        .HasForeignKey("ChannelGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StreamMaster.Domain.Models.StreamGroup", "StreamGroup")
                        .WithMany("ChannelGroups")
                        .HasForeignKey("StreamGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ChannelGroup");

                    b.Navigation("StreamGroup");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupProfile", b =>
                {
                    b.HasOne("StreamMaster.Domain.Models.StreamGroup", null)
                        .WithMany("StreamGroupProfiles")
                        .HasForeignKey("StreamGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroupSMChannelLink", b =>
                {
                    b.HasOne("StreamMaster.Domain.Models.SMChannel", "SMChannel")
                        .WithMany("StreamGroups")
                        .HasForeignKey("SMChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("StreamMaster.Domain.Models.StreamGroup", "StreamGroup")
                        .WithMany("SMChannels")
                        .HasForeignKey("StreamGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMChannel");

                    b.Navigation("StreamGroup");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.SMChannel", b =>
                {
                    b.Navigation("SMChannels");

                    b.Navigation("SMStreams");

                    b.Navigation("StreamGroups");
                });

            modelBuilder.Entity("StreamMaster.Domain.Models.StreamGroup", b =>
                {
                    b.Navigation("ChannelGroups");

                    b.Navigation("SMChannels");

                    b.Navigation("StreamGroupProfiles");
                });
#pragma warning restore 612, 618
        }
    }
}

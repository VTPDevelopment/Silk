﻿// <auto-generated />
using System;
using AnnoucementPlugin.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AnnoucementPlugin.Migrations
{
    [DbContext(typeof(AnnouncementContext))]
    partial class AnnouncementContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.9")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("AnnoucementPlugin.Database.AnnouncementModel", b =>
                {
                    b.Property<int>("PK_Key")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("AnnouncementMessage")
                        .HasMaxLength(4000)
                        .HasColumnType("character varying(4000)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("ScheduledFor")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("PK_Key");

                    b.ToTable("Announcements");
                });
#pragma warning restore 612, 618
        }
    }
}

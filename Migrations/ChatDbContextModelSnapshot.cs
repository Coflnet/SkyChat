﻿// <auto-generated />
using System;
using Coflnet.Sky.Chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace SkyChat.Migrations
{
    [DbContext(typeof(ChatDbContext))]
    partial class ChatDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Coflnet.Sky.Chat.Models.Client", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ApiKey")
                        .HasColumnType("longtext");

                    b.Property<string>("Contact")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Created")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<int>("Quota")
                        .HasColumnType("int");

                    b.Property<string>("WebHook")
                        .HasColumnType("longtext");

                    b.Property<string>("WebhookAuth")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("Coflnet.Sky.Chat.Models.DbMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("ClientId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .HasColumnType("longtext");

                    b.Property<int>("Level")
                        .HasColumnType("int");

                    b.Property<string>("Sender")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<DateTime>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Sender");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("Coflnet.Sky.Chat.Models.Mute", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Message")
                        .HasColumnType("longtext");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Uuid")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.HasKey("Id");

                    b.ToTable("Mute");
                });

            modelBuilder.Entity("Coflnet.Sky.Chat.Models.Report", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("MessageId")
                        .HasColumnType("int");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext");

                    b.Property<string>("Sender")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<DateTime>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("MessageId");

                    b.ToTable("Reports");
                });

            modelBuilder.Entity("Coflnet.Sky.Chat.Models.Report", b =>
                {
                    b.HasOne("Coflnet.Sky.Chat.Models.DbMessage", "Message")
                        .WithMany()
                        .HasForeignKey("MessageId");

                    b.Navigation("Message");
                });
#pragma warning restore 612, 618
        }
    }
}

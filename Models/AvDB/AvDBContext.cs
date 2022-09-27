using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TakeHtml.Models.AvDB
{
    public partial class AvDBContext : DbContext
    {
        public AvDBContext()
        {
        }

        public AvDBContext(DbContextOptions<AvDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PostD> PostDs { get; set; } = null!;
        public virtual DbSet<PostM> PostMs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//                optionsBuilder.UseSqlite("Data Source=./Dbase/AvDB.db");
//            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PostD>(entity =>
            {
                entity.HasKey(e => e.PostDpk);

                entity.ToTable("PostD");

                entity.HasIndex(e => e.PostDname, "IX_PostD_PostDname")
                    .IsUnique();

                entity.HasIndex(e => e.PostDpk, "IX_PostD_PostDpk")
                    .IsUnique();

                entity.Property(e => e.AvBt).HasColumnName("AvBT");

                entity.Property(e => e.CrTime).HasDefaultValueSql("datetime('now', 'localtime')");

                entity.Property(e => e.PostDname).HasDefaultValueSql("hex(randomblob(16))");
            });

            modelBuilder.Entity<PostM>(entity =>
            {
                entity.HasKey(e => e.PostMpk);

                entity.ToTable("PostM");

                entity.HasIndex(e => e.PostMname, "IX_PostM_PostMname")
                    .IsUnique();

                entity.HasIndex(e => e.PostMpk, "IX_PostM_PostMpk")
                    .IsUnique();

                entity.Property(e => e.CrTime).HasDefaultValueSql("datetime('now', 'localtime')");

                entity.Property(e => e.PostMname).HasDefaultValueSql("hex(randomblob(16))");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

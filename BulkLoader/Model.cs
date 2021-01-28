#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Source https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

namespace Model
{
    public class Item
    {
        public Guid Id { get; set; }
        public DateTime Registered { get; set; }
        public string Name { get; set; }
        public string Ext { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string No { get; set; }
        public string Comments { get; set; }

        public List<Item> Parents { get; set; } = new List<Item>();
        public List<Item> Children { get; set; } = new List<Item>();
    }

    public class Link
    {
        public Guid ParentId { get; set; }
        public Item Parent { get; set; }

        public Guid ChildId { get; set; }
        public Item Child { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Link> Links { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DocLibr;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(item => item.Id);

                entity.Property(item => item.Name)
                    .IsRequired();
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Id)
                    .ValueGeneratedNever();
                entity.Property(i => i.Name)
                    .IsRequired();

                entity.HasMany(i => i.Parents)
                    .WithMany(i => i.Children)
                    .UsingEntity<Link>(
                        j => j
                            .HasOne(p => p.Parent)
                            .WithMany()
                            //.WithMany(l => l.Links)
                            .HasForeignKey(p => p.ParentId)
                            .OnDelete(DeleteBehavior.Restrict),
                        j => j
                            .HasOne(c => c.Child)
                            .WithMany()
                            //.WithMany(l => l.Links)
                            .HasForeignKey(c => c.ChildId)
                            .OnDelete(DeleteBehavior.Restrict),
                        j =>
                        {
                            j.HasKey(l => new { l.ParentId, l.ChildId });
                            j.Property(l => l.ParentId)
                                .ValueGeneratedNever();
                            j.Property(l => l.ChildId)
                                .ValueGeneratedNever();
                            j.HasIndex(l => l.ParentId);
                            j.HasIndex(l => l.ChildId);
                            j.ToTable("Links");
                        });
            });
        }
    }
}

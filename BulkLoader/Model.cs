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
    }

    [Index(nameof(PrevId))]
    [Index(nameof(NextId))]
    public class Link
    {
        public int Id { get; set; }
        public Guid PrevId { get; set; }
        public Guid NextId { get; set; }
        public string Path { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Link> Links { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DocLibr;Trusted_Connection=True;");
        }
    }
}

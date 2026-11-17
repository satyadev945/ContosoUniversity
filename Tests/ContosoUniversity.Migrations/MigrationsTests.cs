using System;
using System.Linq;
using System.Reflection;
using ContosoUniversity.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace ContosoUniversity.Migrations
{
    public class InitialCreateTests
    {
        [Fact]
        public void Up_CreatesAllTables()
        {
            var migration = new InitialCreate();
            var tables = migration.UpOperations.OfType<CreateTableOperation>().Select(o => o.Name).ToList();
            Assert.Contains("Student", tables);
            Assert.Contains("Instructor", tables);
            Assert.Contains("Department", tables);
            Assert.Contains("Course", tables);
            Assert.Contains("Enrollment", tables);
            Assert.Contains("OfficeAssignment", tables);
            Assert.Contains("CourseInstructor", tables);
            Assert.Equal(7, tables.Count);
        }

        [Fact]
        public void Down_DropsAllTables()
        {
            var migration = new InitialCreate();
            var dropped = migration.DownOperations.OfType<DropTableOperation>().Select(o => o.Name).ToList();
            Assert.Equal(7, dropped.Count);
            Assert.Contains("Student", dropped);
        }

        [Fact]
        public void Up_CreatesIndexes()
        {
            var migration = new InitialCreate();
            Assert.NotEmpty(migration.UpOperations.OfType<CreateIndexOperation>());
        }

        [Fact]
        public void TargetModel_ContainsEntities()
        {
            var migration = new InitialCreate();
            var model = migration.TargetModel;
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Student"));
            Assert.NotNull(model.FindEntityType("ContosoUniversity.Models.Course"));
        }

        [Fact]
        public void Migration_HasMigrationAttribute()
        {
            var attr = typeof(InitialCreate).GetCustomAttribute<MigrationAttribute>();
            Assert.NotNull(attr);
            Assert.Equal("20261117090411_InitialCreate", attr.Id);
            Assert.Equal(typeof(SchoolContext), typeof(InitialCreate).GetCustomAttribute<DbContextAttribute>().ContextType);
        }
    }

    public class SchoolContextModelSnapshotTests
    {
        private static ModelSnapshot CreateSnapshot()
        {
            var type = typeof(SchoolContext).Assembly.GetType("ContosoUniversity.Migrations.SchoolContextModelSnapshot");
            Assert.NotNull(type);
            return (ModelSnapshot)Activator.CreateInstance(type, nonPublic: true);
        }

        [Fact]
        public void Model_ContainsAllEntityTypes()
        {
            var model = CreateSnapshot().Model;
            foreach (var name in new[] { "Student", "Course", "Department", "Enrollment", "Instructor", "OfficeAssignment" })
            {
                Assert.NotNull(model.FindEntityType("ContosoUniversity.Models." + name));
            }
        }

        [Fact]
        public void Model_HasProductVersionAnnotation()
        {
            var model = CreateSnapshot().Model;
            Assert.Equal("10.0.0", model.FindAnnotation("ProductVersion")?.Value);
        }
    }
}

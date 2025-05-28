using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudentManagement.Data;

namespace StudentManagement.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20240527_InitialCreate")]
partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "7.0.14");

        modelBuilder.Entity("StudentManagement.Models.Course", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("Description")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.ToTable("Courses");
        });

        modelBuilder.Entity("StudentManagement.Models.Student", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("Name")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("PhotoPath")
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.ToTable("Students");
        });

        modelBuilder.Entity("CourseStudent", b =>
        {
            b.Property<int>("CoursesId")
                .HasColumnType("INTEGER");

            b.Property<int>("StudentsId")
                .HasColumnType("INTEGER");

            b.HasKey("CoursesId", "StudentsId");

            b.HasIndex("StudentsId");

            b.ToTable("CourseStudent");
        });

        modelBuilder.Entity("CourseStudent", b =>
        {
            b.HasOne("StudentManagement.Models.Course", null)
                .WithMany()
                .HasForeignKey("CoursesId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("StudentManagement.Models.Student", null)
                .WithMany()
                .HasForeignKey("StudentsId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
} 
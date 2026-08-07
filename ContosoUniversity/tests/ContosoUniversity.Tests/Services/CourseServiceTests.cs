using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Services;
using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Services;

/// <summary>
/// Unit tests for CourseService.
/// </summary>
public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _mockRepository;
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _mockRepository = new Mock<ICourseRepository>();
        _service = new CourseService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllCoursesAsync_ReturnsAllCourses()
    {
        // Arrange
        var courses = new List<Course>
        {
            new() { CourseID = 1050, Title = "Chemistry",      Credits = 3, Department = new Department { Name = "Engineering" } },
            new() { CourseID = 4022, Title = "Microeconomics", Credits = 3, Department = new Department { Name = "Economics" } }
        };
        _mockRepository.Setup(r => r.GetAllWithDepartmentAsync()).ReturnsAsync(courses);

        // Act
        var result = await _service.GetAllCoursesAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetCourseByIdAsync_ReturnsCourse_WhenExists()
    {
        // Arrange
        var course = new Course
        {
            CourseID = 1050,
            Title = "Chemistry",
            Credits = 3,
            Department = new Department { Name = "Engineering" }
        };
        _mockRepository.Setup(r => r.GetByIdWithDepartmentAsync(1050)).ReturnsAsync(course);

        // Act
        var result = await _service.GetCourseByIdAsync(1050);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Chemistry", result.Title);
        Assert.Equal("Engineering", result.DepartmentName);
    }

    [Fact]
    public async Task GetCourseByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdWithDepartmentAsync(999)).ReturnsAsync((Course?)null);

        // Act
        var result = await _service.GetCourseByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCourseAsync_CallsRepositoryAdd()
    {
        // Arrange
        var courseDto = new CourseDto { CourseID = 9999, Title = "New Course", Credits = 3, DepartmentID = 1 };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCourseAsync(courseDto);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Once);
        Assert.Equal("New Course", result.Title);
    }

    [Fact]
    public async Task DeleteCourseAsync_ReturnsFalse_WhenCourseNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteCourseAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCourseAsync_ReturnsTrue_WhenCourseExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(1050)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.DeleteAsync(1050)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteCourseAsync(1050);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(1050), Times.Once);
    }
}

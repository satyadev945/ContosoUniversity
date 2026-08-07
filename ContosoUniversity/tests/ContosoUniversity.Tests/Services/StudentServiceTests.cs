using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Services;
using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Services;

/// <summary>
/// Unit tests for StudentService.
/// Tests business logic in isolation using Moq for repository dependencies.
/// </summary>
public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _mockRepository;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _mockRepository = new Mock<IStudentRepository>();
        _service = new StudentService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsAllStudents_WhenNoFilter()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = DateTime.Parse("2010-09-01") },
            new() { ID = 2, LastName = "Alonso",    FirstMidName = "Meredith", EnrollmentDate = DateTime.Parse("2012-09-01") }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(students);

        // Act
        var result = await _service.GetAllStudentsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllStudentsAsync_FiltersStudents_WhenSearchStringProvided()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { ID = 1, LastName = "Alexander", FirstMidName = "Carson",   EnrollmentDate = DateTime.Now },
            new() { ID = 2, LastName = "Alonso",    FirstMidName = "Meredith", EnrollmentDate = DateTime.Now }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(students);

        // Act
        var result = await _service.GetAllStudentsAsync(searchString: "Alex");

        // Assert
        Assert.Single(result);
        Assert.Equal("Alexander", result.First().LastName);
    }

    [Fact]
    public async Task GetAllStudentsAsync_SortsDescending_WhenSortOrderIsNameDesc()
    {
        // Arrange
        var students = new List<Student>
        {
            new() { ID = 1, LastName = "Alexander", FirstMidName = "Carson",   EnrollmentDate = DateTime.Now },
            new() { ID = 2, LastName = "Zheng",     FirstMidName = "Roger",    EnrollmentDate = DateTime.Now }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(students);

        // Act
        var result = (await _service.GetAllStudentsAsync(sortOrder: "name_desc")).ToList();

        // Assert
        Assert.Equal("Zheng", result[0].LastName);
        Assert.Equal("Alexander", result[1].LastName);
    }

    [Fact]
    public async Task GetStudentByIdAsync_ReturnsStudent_WhenExists()
    {
        // Arrange
        var student = new Student { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = DateTime.Now };
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);

        // Act
        var result = await _service.GetStudentByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alexander", result.LastName);
    }

    [Fact]
    public async Task GetStudentByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        // Act
        var result = await _service.GetStudentByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateStudentAsync_CallsRepositoryAdd()
    {
        // Arrange
        var studentDto = new StudentDto
        {
            LastName = "NewStudent",
            FirstMidName = "Test",
            EnrollmentDate = DateTime.Now
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Student>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateStudentAsync(studentDto);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once);
        Assert.Equal("NewStudent", result.LastName);
    }

    [Fact]
    public async Task UpdateStudentAsync_ReturnsFalse_WhenStudentNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        // Act
        var result = await _service.UpdateStudentAsync(new StudentDto { ID = 999 });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateStudentAsync_ReturnsTrue_WhenStudentExists()
    {
        // Arrange
        var student = new Student { ID = 1, LastName = "Old", FirstMidName = "Name", EnrollmentDate = DateTime.Now };
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Student>())).Returns(Task.CompletedTask);

        var dto = new StudentDto { ID = 1, LastName = "New", FirstMidName = "Name", EnrollmentDate = DateTime.Now };

        // Act
        var result = await _service.UpdateStudentAsync(dto);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Student>()), Times.Once);
    }

    [Fact]
    public async Task DeleteStudentAsync_ReturnsFalse_WhenStudentNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteStudentAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteStudentAsync_ReturnsTrue_WhenStudentExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _mockRepository.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteStudentAsync(1);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetEnrollmentStatisticsAsync_GroupsStudentsByDate()
    {
        // Arrange
        var date1 = DateTime.Parse("2010-09-01");
        var date2 = DateTime.Parse("2012-09-01");
        var students = new List<Student>
        {
            new() { ID = 1, LastName = "A", FirstMidName = "A", EnrollmentDate = date1 },
            new() { ID = 2, LastName = "B", FirstMidName = "B", EnrollmentDate = date2 },
            new() { ID = 3, LastName = "C", FirstMidName = "C", EnrollmentDate = date2 }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(students);

        // Act
        var result = (await _service.GetEnrollmentStatisticsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result.First(g => g.EnrollmentDate == date1).StudentCount);
        Assert.Equal(2, result.First(g => g.EnrollmentDate == date2).StudentCount);
    }
}

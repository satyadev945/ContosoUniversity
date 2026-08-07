using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContosoUniversity.Tests.Controllers;

/// <summary>
/// Unit tests for StudentsController.
/// Tests controller actions in isolation using Moq for service dependencies.
/// </summary>
public class StudentsControllerTests
{
    private readonly Mock<IStudentService> _mockService;
    private readonly Mock<ILogger<StudentsController>> _mockLogger;
    private readonly StudentsController _controller;

    public StudentsControllerTests()
    {
        _mockService = new Mock<IStudentService>();
        _mockLogger = new Mock<ILogger<StudentsController>>();
        _controller = new StudentsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithPagedStudents()
    {
        // Arrange
        var students = new List<StudentDto>
        {
            new() { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = DateTime.Now },
            new() { ID = 2, LastName = "Alonso",    FirstMidName = "Meredith", EnrollmentDate = DateTime.Now }
        };
        _mockService.Setup(s => s.GetAllStudentsAsync(null, null)).ReturnsAsync(students);

        // Act
        var result = await _controller.Index(null!, null!, null!, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
    }

    [Fact]
    public async Task Details_ReturnsBadRequest_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Details(null);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetStudentWithEnrollmentsAsync(999)).ReturnsAsync((StudentDto?)null);

        // Act
        var result = await _controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsViewResult_WhenStudentExists()
    {
        // Arrange
        var student = new StudentDto { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = DateTime.Now };
        _mockService.Setup(s => s.GetStudentWithEnrollmentsAsync(1)).ReturnsAsync(student);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentDto>(viewResult.Model);
        Assert.Equal("Alexander", model.LastName);
    }

    [Fact]
    public void Create_ReturnsViewResult_OnGet()
    {
        // Act
        var result = _controller.Create();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_RedirectsToIndex_WhenModelIsValid()
    {
        // Arrange
        var studentDto = new StudentDto { LastName = "New", FirstMidName = "Student", EnrollmentDate = DateTime.Now };
        _mockService.Setup(s => s.CreateStudentAsync(It.IsAny<StudentDto>())).ReturnsAsync(studentDto);
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.Create(studentDto);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Delete(null);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenStudentDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetStudentByIdAsync(999)).ReturnsAsync((StudentDto?)null);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletePost_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteStudentAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }
}

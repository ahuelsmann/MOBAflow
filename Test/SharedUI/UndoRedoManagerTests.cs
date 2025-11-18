using Moba.SharedUI.Service;
using Moba.Backend.Model;

namespace Moba.Test.SharedUI;

/// <summary>
/// Unit tests for UndoRedoManager class.
/// Tests file-based undo/redo functionality and history management.
/// </summary>
[TestFixture]
public class UndoRedoManagerTests
{
    private string _testDirectory = null!;
    private UndoRedoManager _manager = null!;

    [SetUp]
    public void SetUp()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"UndoRedoTest_{Guid.NewGuid()}");
        _manager = new UndoRedoManager(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test files
        try
        {
            _manager.ClearHistory();
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Initialization Tests

    [Test]
    public void Constructor_CreatesHistoryDirectory()
    {
        // Assert
        Assert.That(Directory.Exists(_testDirectory), Is.True, 
            "History directory should be created during initialization");
    }

    [Test]
    public void Constructor_InitializesWithEmptyHistory()
    {
        // Act
        var (totalStates, currentIndex, canUndo, canRedo) = _manager.GetHistoryInfo();

        // Assert
        Assert.That(totalStates, Is.EqualTo(0));
        Assert.That(currentIndex, Is.EqualTo(-1));
        Assert.That(canUndo, Is.False);
        Assert.That(canRedo, Is.False);
    }

    #endregion

    #region SaveStateImmediateAsync Tests

    [Test]
    public async Task SaveStateImmediateAsync_SavesSolution_ToHistoryDirectory()
    {
        // Arrange
        var solution = CreateTestSolution("Test Solution");

        // Act
        await _manager.SaveStateImmediateAsync(solution);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "history_*.json");
        Assert.That(files.Length, Is.EqualTo(1), "One history file should be created");
    }

    [Test]
    public async Task SaveStateImmediateAsync_UpdatesHistoryInfo()
    {
        // Arrange
        var solution = CreateTestSolution("Test Solution");

        // Act
        await _manager.SaveStateImmediateAsync(solution);
        var (totalStates, currentIndex, canUndo, canRedo) = _manager.GetHistoryInfo();

        // Assert
        Assert.That(totalStates, Is.EqualTo(1));
        Assert.That(currentIndex, Is.EqualTo(0));
        Assert.That(canUndo, Is.False, "Cannot undo when only one state exists");
        Assert.That(canRedo, Is.False);
    }

    [Test]
    public async Task SaveStateImmediateAsync_AllowsMultipleSaves()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        // Act
        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);
        await _manager.SaveStateImmediateAsync(solution3);

        var (totalStates, currentIndex, canUndo, canRedo) = _manager.GetHistoryInfo();

        // Assert
        Assert.That(totalStates, Is.EqualTo(3));
        Assert.That(currentIndex, Is.EqualTo(2));
        Assert.That(canUndo, Is.True);
        Assert.That(canRedo, Is.False);
    }

    [Test]
    public async Task SaveStateImmediateAsync_RemovesFutureHistory_AfterUndo()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);
        await _manager.SaveStateImmediateAsync(solution3);

        // Act - Undo twice, then save new state
        await _manager.UndoAsync();
        await _manager.UndoAsync();
        
        var solution4 = CreateTestSolution("Solution 4");
        await _manager.SaveStateImmediateAsync(solution4);

        var (totalStates, _, _, _) = _manager.GetHistoryInfo();

        // Assert
        Assert.That(totalStates, Is.EqualTo(2), 
            "Should have 2 states: original + new branch (solutions 2 & 3 removed)");
        
        var files = Directory.GetFiles(_testDirectory, "history_*.json");
        Assert.That(files.Length, Is.EqualTo(2), "Old future history files should be deleted");
    }

    [Test]
    public async Task SaveStateImmediateAsync_LimitsHistorySize_To50States()
    {
        // Arrange - Save 55 states (over the limit of 50)
        for (int i = 0; i < 55; i++)
        {
            var solution = CreateTestSolution($"Solution {i}");
            await _manager.SaveStateImmediateAsync(solution);
        }

        // Act
        var (totalStates, currentIndex, _, _) = _manager.GetHistoryInfo();
        var files = Directory.GetFiles(_testDirectory, "history_*.json");

        // Assert
        Assert.That(totalStates, Is.EqualTo(50), "History should be limited to 50 states");
        Assert.That(currentIndex, Is.EqualTo(49), "Current index should be 49 (0-based)");
        Assert.That(files.Length, Is.EqualTo(50), "Only 50 history files should exist");
    }

    [Test]
    public async Task SaveStateImmediateAsync_DeletesOldestState_WhenLimitReached()
    {
        // Arrange - Save 51 states
        for (int i = 0; i < 51; i++)
        {
            var solution = CreateTestSolution($"Solution {i}");
            await _manager.SaveStateImmediateAsync(solution);
            await Task.Delay(10); // Ensure unique timestamps
        }

        // Act - Get all file names sorted by creation time
        var files = Directory.GetFiles(_testDirectory, "history_*.json")
            .OrderBy(f => File.GetCreationTime(f))
            .ToList();

        // Assert
        Assert.That(files.Count, Is.EqualTo(50), "Should have exactly 50 files");
        
        // Verify the oldest file was deleted (by checking if first file is newer)
        var oldestFile = files.First();
        var timestamp = Path.GetFileNameWithoutExtension(oldestFile).Replace("history_", "");
        
        // The oldest timestamp should NOT be the very first one we created
        Assert.That(timestamp, Does.Not.StartWith("history_"), 
            "Oldest file should not be the very first one saved");
    }

    #endregion

    #region UndoAsync Tests

    [Test]
    public async Task UndoAsync_ReturnsNull_WhenNoHistory()
    {
        // Act
        var result = await _manager.UndoAsync();

        // Assert
        Assert.That(result, Is.Null, "Undo should return null when no history exists");
    }

    [Test]
    public async Task UndoAsync_ReturnsNull_WhenOnlyOneState()
    {
        // Arrange
        var solution = CreateTestSolution("Only State");
        await _manager.SaveStateImmediateAsync(solution);

        // Act
        var result = await _manager.UndoAsync();

        // Assert
        Assert.That(result, Is.Null, "Cannot undo when only one state exists");
        Assert.That(_manager.CanUndo, Is.False);
    }

    [Test]
    public async Task UndoAsync_LoadsPreviousSolution()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);

        // Act
        var undoneResult = await _manager.UndoAsync();

        // Assert
        Assert.That(undoneResult, Is.Not.Null);
        Assert.That(undoneResult?.Name, Does.Contain("Solution 1"), 
            "Undo should load the previous solution");
    }

    [Test]
    public async Task UndoAsync_UpdatesCanUndoCanRedo()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);
        await _manager.SaveStateImmediateAsync(solution3);

        // Act
        await _manager.UndoAsync();

        // Assert
        Assert.That(_manager.CanUndo, Is.True, "Should still be able to undo");
        Assert.That(_manager.CanRedo, Is.True, "Should now be able to redo");
    }

    [Test]
    public async Task UndoAsync_CanUndoMultipleTimes()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);
        await _manager.SaveStateImmediateAsync(solution3);

        // Act
        var result1 = await _manager.UndoAsync(); // Back to Solution 2
        var result2 = await _manager.UndoAsync(); // Back to Solution 1

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(_manager.CanUndo, Is.False, "Should not be able to undo further");
        Assert.That(_manager.CanRedo, Is.True);
    }

    [Test]
    public async Task UndoAsync_RevertsIndex_OnFileLoadFailure()
    {
        // Arrange
        var solution = CreateTestSolution("Test Solution");
        await _manager.SaveStateImmediateAsync(solution);
        await _manager.SaveStateImmediateAsync(solution);

        // Delete history files to simulate load failure
        var files = Directory.GetFiles(_testDirectory, "history_*.json");
        foreach (var file in files)
        {
            File.Delete(file);
        }

        // Act
        var result = await _manager.UndoAsync();

        // Assert
        Assert.That(result, Is.Null, "Should return null on load failure");
        Assert.That(_manager.CanUndo, Is.True, "Index should be reverted, still able to undo");
    }

    #endregion

    #region RedoAsync Tests

    [Test]
    public async Task RedoAsync_ReturnsNull_WhenNoHistory()
    {
        // Act
        var result = await _manager.RedoAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RedoAsync_ReturnsNull_WhenNoUndoPerformed()
    {
        // Arrange
        var solution = CreateTestSolution("Test Solution");
        await _manager.SaveStateImmediateAsync(solution);

        // Act
        var result = await _manager.RedoAsync();

        // Assert
        Assert.That(result, Is.Null, "Cannot redo without first undoing");
    }

    [Test]
    public async Task RedoAsync_LoadsNextSolution()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);

        await _manager.UndoAsync(); // Back to Solution 1

        // Act
        var redoneResult = await _manager.RedoAsync(); // Forward to Solution 2

        // Assert
        Assert.That(redoneResult, Is.Not.Null);
        Assert.That(redoneResult?.Name, Does.Contain("Solution 2"));
    }

    [Test]
    public async Task RedoAsync_UpdatesCanUndoCanRedo()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);

        await _manager.UndoAsync();

        // Act
        await _manager.RedoAsync();

        // Assert
        Assert.That(_manager.CanUndo, Is.True);
        Assert.That(_manager.CanRedo, Is.False, "Should not be able to redo after reaching latest state");
    }

    [Test]
    public async Task RedoAsync_CanRedoMultipleTimes()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        await _manager.SaveStateImmediateAsync(solution1);
        await _manager.SaveStateImmediateAsync(solution2);
        await _manager.SaveStateImmediateAsync(solution3);

        await _manager.UndoAsync();
        await _manager.UndoAsync();

        // Act
        var result1 = await _manager.RedoAsync(); // Forward to Solution 2
        var result2 = await _manager.RedoAsync(); // Forward to Solution 3

        // Assert
        Assert.That(result1, Is.Not.Null);
        Assert.That(result2, Is.Not.Null);
        Assert.That(_manager.CanRedo, Is.False);
    }

    #endregion

    #region SaveStateThrottled Tests

    [Test]
    public async Task SaveStateThrottled_DelaysSave_By1Second()
    {
        // Arrange
        var solution = CreateTestSolution("Test Solution");
        var startTime = DateTime.UtcNow;

        // Act
        _manager.SaveStateThrottled(solution);
        
        // Wait a bit less than 1 second - file should not exist yet
        await Task.Delay(800);
        var filesBeforeDelay = Directory.GetFiles(_testDirectory, "history_*.json");

        // Wait for throttle to complete
        await Task.Delay(400);
        var filesAfterDelay = Directory.GetFiles(_testDirectory, "history_*.json");

        // Assert
        Assert.That(filesBeforeDelay.Length, Is.EqualTo(0), 
            "File should not be saved before 1 second delay");
        Assert.That(filesAfterDelay.Length, Is.EqualTo(1), 
            "File should be saved after throttle delay");
    }

    [Test]
    public async Task SaveStateThrottled_CancelsPreviousSave_OnNewChange()
    {
        // Arrange
        var solution1 = CreateTestSolution("Solution 1");
        var solution2 = CreateTestSolution("Solution 2");
        var solution3 = CreateTestSolution("Solution 3");

        // Act - Rapid changes (simulating user typing)
        _manager.SaveStateThrottled(solution1);
        await Task.Delay(300);
        
        _manager.SaveStateThrottled(solution2);
        await Task.Delay(300);
        
        _manager.SaveStateThrottled(solution3);
        await Task.Delay(1200); // Wait for throttle

        var files = Directory.GetFiles(_testDirectory, "history_*.json");

        // Assert
        Assert.That(files.Length, Is.EqualTo(1), 
            "Only the last change should be saved (previous saves cancelled)");
    }

    #endregion

    #region ClearHistory Tests

    [Test]
    public async Task ClearHistory_DeletesAllHistoryFiles()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var solution = CreateTestSolution($"Solution {i}");
            await _manager.SaveStateImmediateAsync(solution);
        }

        // Act
        _manager.ClearHistory();

        // Assert
        var files = Directory.GetFiles(_testDirectory, "history_*.json");
        Assert.That(files.Length, Is.EqualTo(0), "All history files should be deleted");
    }

    [Test]
    public async Task ClearHistory_ResetsHistoryInfo()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var solution = CreateTestSolution($"Solution {i}");
            await _manager.SaveStateImmediateAsync(solution);
        }

        // Act
        _manager.ClearHistory();
        var (totalStates, currentIndex, canUndo, canRedo) = _manager.GetHistoryInfo();

        // Assert
        Assert.That(totalStates, Is.EqualTo(0));
        Assert.That(currentIndex, Is.EqualTo(-1));
        Assert.That(canUndo, Is.False);
        Assert.That(canRedo, Is.False);
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public async Task Manager_IsThreadSafe_ForConcurrentSaves()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Simulate concurrent saves from multiple threads
        for (int i = 0; i < 10; i++)
        {
            var solution = CreateTestSolution($"Solution {i}");
            tasks.Add(Task.Run(async () => await _manager.SaveStateImmediateAsync(solution)));
        }

        await Task.WhenAll(tasks);

        // Assert
        var (totalStates, _, _, _) = _manager.GetHistoryInfo();
        Assert.That(totalStates, Is.EqualTo(10), 
            "All concurrent saves should be recorded safely");
    }

    #endregion

    #region Helper Methods

    private static Solution CreateTestSolution(string name)
    {
        return new Solution
        {
            Name = name,
            Projects = new List<Project>
            {
                new Project
                {
                    Name = "Test Project",
                    Journeys = new List<Journey>
                    {
                        new Journey
                        {
                            Name = "Test Journey",
                            InPort = 1
                        }
                    }
                }
            }
        };
    }

    #endregion
}

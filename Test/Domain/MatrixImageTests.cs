// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Tests.Domain;

using Moba.Domain;

using System.Text.Json;

[TestFixture]
public sealed class MatrixImageTests
{
    [Test]
    public void Constructor_Should_Initialize_DefaultCells()
    {
        var image = new MatrixImage();

        Assert.Multiple(() =>
        {
            Assert.That(image.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(image.Name, Is.Empty);
            Assert.That(image.Cells, Has.Count.EqualTo(MatrixImage.CellCount));
            Assert.That(image.Cells, Is.All.EqualTo(MatrixImage.OffColorArgb));
        });
    }

    [Test]
    public void NormalizeCells_Should_Pad_MissingCells()
    {
        var image = new MatrixImage { Cells = [0xFFFF0000] };

        image.NormalizeCells();

        Assert.Multiple(() =>
        {
            Assert.That(image.Cells, Has.Count.EqualTo(MatrixImage.CellCount));
            Assert.That(image.Cells[0], Is.EqualTo(0xFFFF0000));
            Assert.That(image.Cells[1], Is.EqualTo(MatrixImage.OffColorArgb));
        });
    }

    [Test]
    public void Project_Should_Initialize_MatricesCollection()
    {
        var project = new Project();

        Assert.That(project.Matrices, Is.Not.Null);
        Assert.That(project.Matrices, Is.Empty);
    }

    [Test]
    public void SolutionJson_Should_Roundtrip_ProjectMatrices()
    {
        var solution = new Solution();
        var project = new Project { Name = "Demo" };
        var matrix = new MatrixImage { Name = "Signal" };
        matrix.Cells[0] = 0xFFFF0000;
        project.Matrices.Add(matrix);
        solution.Projects.Add(project);

        var json = JsonSerializer.Serialize(solution, JsonOptions.Default);
        var reloaded = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default);

        Assert.That(reloaded, Is.Not.Null);
        var reloadedMatrix = reloaded!.Projects.Single().Matrices.Single();
        Assert.Multiple(() =>
        {
            Assert.That(reloadedMatrix.Name, Is.EqualTo("Signal"));
            Assert.That(reloadedMatrix.Cells, Has.Count.EqualTo(MatrixImage.CellCount));
            Assert.That(reloadedMatrix.Cells[0], Is.EqualTo(0xFFFF0000));
        });
    }
}

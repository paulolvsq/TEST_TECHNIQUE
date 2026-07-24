using System.IO.Abstractions;

namespace Distributor.Data;

public interface IDistributorDatabaseFileProvider
{
    IFileInfo GetDatabaseFile();
}

public sealed class DistributorDatabaseFileProvider : IDistributorDatabaseFileProvider
{
    private readonly IFileSystem _fileSystem;

    public DistributorDatabaseFileProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public IFileInfo GetDatabaseFile()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = _fileSystem.Path.Combine(root, "Distributor", "distributor.db");

        return _fileSystem.FileInfo.New(path);
    }
}

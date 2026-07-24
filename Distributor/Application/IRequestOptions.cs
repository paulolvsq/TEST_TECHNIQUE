using System.CommandLine;

namespace Distributor.Application;

public interface IRequestOptions<out TRequest>
{
    string Name { get; }
    string Description { get; }

    IEnumerable<Option> Enumerate();
    TRequest Parse(ParseResult result);
}

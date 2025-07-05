using GS1DigitalLink.Model;

namespace GS1DigitalLink.Processors;

public interface IDigitalLinkParser
{
    void Parse(string input, DigitalLink result);
    void Parse(Uri input, DigitalLink result);
}

using System.Text;

namespace GS1DigitalLink.Utils;

public class BitStream(string compressedString)
{
    private int _position;
    private string _buffer = string.Empty;

    public int Length => compressedString.Length*6;
    public int Position => _position;
    public string Current { get; private set; } = string.Empty;

    public void Buffer(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Length, _position + length);
    
        var output = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            output.Append(ReadChar());
        }

        Current = output.ToString();
    }

    private char ReadChar()
    {
        if (_position % 6 == 0)
        {
            _buffer = Characters.GetBinary(compressedString.ElementAt(_position / 6));
        }

        return _buffer[_position++ % 6];
    }
}

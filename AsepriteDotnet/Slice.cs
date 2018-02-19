namespace Aseprite
{
    public class Slice
    {
        private byte[] data;

        private Slice(byte[] data)
        {
            this.data = data;
        }

        internal static Slice FromChunk(Chunk chunk)
        {
            return new Slice(chunk.Data);
        }
    }
}

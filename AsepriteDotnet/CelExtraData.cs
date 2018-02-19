namespace Aseprite
{
    public class CelExtraData
    {
        private byte[] data;

        private CelExtraData(byte[] data)
        {
            this.data = data;
        }

        internal static CelExtraData FromChunk(Chunk chunk)
        {
            return new CelExtraData(chunk.Data);
        }
    }
}

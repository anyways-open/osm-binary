using System.IO;

namespace OsmSharp.IO.Binary.Test.v0_1
{
    public static class TestStreams
    {
        public static Stream GetData1Stream()
        {
            return TestStreams.LoadAsStream("OsmSharp.IO.Binary.Test.v0_1.test_data.data1.osm.bin");
        }
        
        public static Stream LoadAsStream(string path)
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
                path);
        }
    }
}
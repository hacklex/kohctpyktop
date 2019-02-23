using System.IO;
using System.IO.Compression;
using JsonSubTypes;
using Kohctpyktop.Models.Field;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Kohctpyktop.Serialization
{
    public static class LayerSerializer
    {
        // todo: async methods

        private static readonly JsonSerializerSettings Settings = BuildSerializerSettings();
        
        private static JsonSerializerSettings BuildSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            
            settings.Converters.Add(new StringEnumConverter());
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(ValuesFunction), nameof(ValuesFunction.Type))
                .RegisterSubtype(typeof(StaticValuesFunction), ValuesFunctionType.Static)
                .RegisterSubtype(typeof(PeriodicValuesFunction), ValuesFunctionType.Periodic)
                .Build());

            return settings;
        }

        public static ILayer ReadLayer(Stream stream)
        {
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                var json = reader.ReadToEnd();
                var layerData = JsonConvert.DeserializeObject<LayerData>(json, Settings);
                
                return new Layer(layerData);
            }
        } 
        
        public static void WriteLayer(Stream stream, ILayer layer)
        {
            var layerData = layer.ExportLayerData();
            var json = JsonConvert.SerializeObject(layerData, BuildSerializerSettings());

            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzipStream))
                writer.Write(json);
        } 
    }
}
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlantFlow_Support
{
    // Phase 1a: drawing 채널 봉투 파싱 + 응답 직렬화. 실제 연산은 PaletteTab(상태 소유)이 수행.
    internal static class DrawingBridge
    {
        internal sealed class Envelope
        {
            [JsonProperty("ch")] public string Ch;
            [JsonProperty("id")] public string Id;
            [JsonProperty("method")] public string Method;
            [JsonProperty("args")] public JArray Args;
        }

        // ch가 "drawing"이 아니면 null(다른 채널로 넘어가게).
        internal static Envelope Parse(string msg, Action<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(msg) || msg.IndexOf("\"drawing\"", StringComparison.Ordinal) < 0) return null;
                var e = JsonConvert.DeserializeObject<Envelope>(msg);
                return (e != null && e.Ch == "drawing") ? e : null;
            }
            catch (Exception ex)
            {
                log("DrawingBridge parse 실패: " + ex.Message);
                return null;
            }
        }

        internal static string Ok(string id, object result) =>
            JsonConvert.SerializeObject(new { ch = "drawing", id, ok = true, result });

        // 응답 계약 고정: { ok:false, code, error }.
        internal static string Fail(string id, string code, string error) =>
            JsonConvert.SerializeObject(new { ch = "drawing", id, ok = false, code, error });
    }
}

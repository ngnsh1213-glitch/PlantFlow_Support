using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace PlantFlow_Support
{
    public class SpanTable
    {
        private Dictionary<string, List<double>> max_span;
        private Dictionary<string, object> _meta;

        public static SpanTable Load(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Span table file not found at: {jsonPath}");
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (rawData == null)
                {
                    throw new InvalidDataException("JSON parsing resulted in null.");
                }

                var table = new SpanTable
                {
                    max_span = JsonConvert.DeserializeObject<Dictionary<string, List<double>>>(rawData["max_span"].ToString()),
                    _meta = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData["_meta"].ToString())
                };

                return table;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to parse span table JSON: {ex.Message}", ex);
            }
        }

        // span_table_JIS.json 표준 위치 탐색(공용). 못 찾으면 null.
        // 어셈블리 디렉터리에서 상위로 올라가며 Support\library\span_table_JIS.json 탐색 + 개발경로 폴백.
        public static string ResolveDefaultPath()
        {
            const string rel = @"Support\library\span_table_JIS.json";
            var candidates = new List<string>();
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    candidates.Add(Path.Combine(dir, rel));
                    candidates.Add(Path.Combine(dir, "span_table_JIS.json"));
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch { /* 어셈블리 경로 취득 실패는 개발경로 폴백으로 */ }

            // 개발경로 폴백.
            candidates.Add(@"D:\PlantFlow\PlantFlow_Support\PlantFlow_Support\Support\library\span_table_JIS.json");

            foreach (string c in candidates)
            {
                try { if (File.Exists(c)) return c; }
                catch { /* 개별 후보 접근 실패는 다음 후보로 */ }
            }
            return null;
        }

        // 테스트/주입용: 파일 없이 메모리 dict로 구성.
        public static SpanTable FromMemory(Dictionary<string, List<double>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new SpanTable { max_span = data, _meta = new Dictionary<string, object>() };
        }

        public bool HasDn(int dnJis)
        {
            return max_span != null && max_span.ContainsKey(dnJis.ToString());
        }

        public double MaxSpan(int dnJis, LoadCase c)
        {
            string key = dnJis.ToString();
            if (!HasDn(dnJis))
            {
                throw new ArgumentOutOfRangeException(nameof(dnJis), $"DN {dnJis} is not allowed or not found in span table. Interpolation is forbidden.");
            }

            int caseIndex = (int)c;
            var values = max_span[key];
            if (caseIndex < 0 || caseIndex >= values.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(c), $"LoadCase {(int)c} is out of range for span table.");
            }

            return values[caseIndex];
        }
    }
}

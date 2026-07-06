using System;

namespace PlantFlow_Support
{
    public class UISettings : IDisposable
    {
        public string OrthoView { get; set; } = "Top";
        public string OrthoScale { get; set; } = "1:50";
        public bool RegenDwgOnVarChange { get; set; } = false;
        public bool DisplayInsulation { get; set; } = true;
        public bool DisplayMarkers { get; set; } = false;
        public bool DisplayPlaceholder { get; set; } = true;

        public UISettings()
        {
        }

        public int GetProjectUnit()
        {
            // Defaulting to Metric (1) to be safe for now. 
            // 1 = Metric, 0 = Imperial (likely)
            return 1; 
        }

        public void Dispose()
        {
            // No unmanaged resources to release
        }
    }
}

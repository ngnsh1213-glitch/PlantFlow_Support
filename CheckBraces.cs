using System;
using System.IO;

class Program
{
    static void Main()
    {
        string[] lines = File.ReadAllLines(@"C:\Users\HT노승환\Documents\PlantFlow_Support\PlantFlow_Support\PlantOrthoView.cs");
        int braceCount = 0;
        int methodStartLine = -1;
        bool inMethod = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Contains("public void ProccessCurrentViewport"))
            {
                methodStartLine = i + 1;
                inMethod = true;
                Console.WriteLine($"Method starts at line {methodStartLine}");
            }

            if (inMethod)
            {
                foreach (char c in line)
                {
                    if (c == '{') braceCount++;
                    if (c == '}') braceCount--;
                }

                if (braceCount == 0 && inMethod)
                {
                    Console.WriteLine($"Method potentially closed at line {i + 1}");
                    // Continue checking to see if it drops below zero or stays zero
                    // If it stays zero and we see more code logic, that's the error.
                    // But technically braceCount==0 means we are back to Class Level.
                    // So any line after this that is NOT a method/field decl is an error.
                    
                    // Let's check next non-empty line
                    int j = i + 1;
                    while(j < lines.Length && string.IsNullOrWhiteSpace(lines[j])) j++;
                    if (j < lines.Length)
                    {
                         // If next line is a statement (e.g. mdiActiveDocument...), it's the bug.
                         Console.WriteLine($"Line {j+1}: {lines[j]}");
                         return;
                    }
                }
            }
        }
    }
}

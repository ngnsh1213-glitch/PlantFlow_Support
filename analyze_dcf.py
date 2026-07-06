import sqlite3
import pandas as pd
import sys

dcf_path = r"C:\Users\HT노승환\Documents\PlantFlow_Support\Piping.dcf"
output_path = r"C:\Users\HT노승환\Documents\PlantFlow_Support\dcf_analysis.txt"

def analyze_dcf():
    with open(output_path, 'w', encoding='utf-8') as f:
        try:
            conn = sqlite3.connect(dcf_path)
            cursor = conn.cursor()
            
            f.write(f"Connected to database: {dcf_path}\n")
            
            # 1. List all tables
            cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
            tables = [row[0] for row in cursor.fetchall()]
            f.write(f"\nTotal Tables: {len(tables)}\n")
            f.write("-" * 30 + "\n")
            for table in sorted(tables):
                f.write(table + "\n")
                
            # 2. Search for Property Definitions
            # PnPProperties, PnPColumnRegistry, PnPClassDefinitions often hold metadata
            metadata_tables = ["PnPTagFormat", "PnPTagRegistries", "PnPTagRegistry", "PnPTagEnlistedColumns"]
            for target in metadata_tables:
                if target in tables:
                   f.write(f"\nScanning Metadata Table: {target}\n")
                   try:
                       cursor.execute(f"PRAGMA table_info({target})")
                       columns = [info[1] for info in cursor.fetchall()] # metadata about metadata table
                       f.write(f"Columns: {columns}\n")
                       
                       # Dump content if small
                       df = pd.read_sql_query(f"SELECT * FROM {target}", conn)
                       f.write(df.to_string() + "\n")
                   except Exception as e:
                       f.write(f"Error reading {target}: {e}\n")
            
            # 3. Analyze Support table structure
            f.write("\n\n=== SUPPORT TABLE ANALYSIS ===\n")
            if "Support" in tables:
                try:
                    cursor.execute("PRAGMA table_info(Support)")
                    support_cols = cursor.fetchall()
                    f.write("\nSupport table columns:\n")
                    for col in support_cols:
                        f.write(f"  {col[1]} ({col[2]})\n")
                    
                    # Get sample data
                    f.write("\nSupport table sample (first 5 rows):\n")
                    df_support = pd.read_sql_query("SELECT * FROM Support LIMIT 5", conn)
                    f.write(df_support.to_string() + "\n")
                except Exception as e:
                    f.write(f"Error analyzing Support table: {e}\n")

            conn.close()
            
        except Exception as e:
            f.write(f"Error accessing database: {e}\n")

if __name__ == "__main__":
    analyze_dcf()

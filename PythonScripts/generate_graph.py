import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import os

# Step 1: Connect to your local SQL Server database
conn = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=(localdb)\\MSSQLLocalDB;'
    'DATABASE=AttendanceSystem;'
    'Trusted_Connection=yes;'
)

# Step 2: Query attendance data
query = """
    SELECT SubjectId, COUNT(*) AS TotalAttendance
    FROM Attendances
    GROUP BY SubjectId
"""
df = pd.read_sql(query, conn)

# Step 3: Generate the bar chart
plt.figure(figsize=(6, 4))
sns.barplot(data=df, x="SubjectId", y="TotalAttendance")
plt.title("Attendance Count per Subject")

# Step 4: Ensure the path exists
graph_dir = os.path.join("wwwroot", "graphs")
os.makedirs(graph_dir, exist_ok=True)
output_path = os.path.join(graph_dir, "graph.png")

# Step 5: Save the image
plt.tight_layout()
plt.savefig(output_path)

print("Graph saved to", output_path)

import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import os

# Connect to SQL Server
conn = pyodbc.connect(
    "Driver={SQL Server};"
    "Server=(localdb)\\MSSQLLocalDB;"
    "Database=AttendanceSystem;"
    "Trusted_Connection=yes;"
)

# Query attendance data
query = """
SELECT s.Name AS StudentName, sub.Name AS SubjectName, a.Date
FROM Attendances a
JOIN Students s ON a.StudentID = s.StudentID
JOIN Subjects sub ON a.SubjectID = sub.SubjectID
"""

df = pd.read_sql(query, conn)

# Calculate attendance percentage
attendance_summary = df.groupby(['StudentName', 'SubjectName']).size().unstack(fill_value=0)

# Plot
plt.figure(figsize=(12, 6))
sns.heatmap(attendance_summary, annot=True, cmap='Blues')
plt.title('Student Attendance Per Subject')

# Save
output_dir = "wwwroot/graphs"
os.makedirs(output_dir, exist_ok=True)
plt.savefig(f"{output_dir}/attendance_heatmap.png")

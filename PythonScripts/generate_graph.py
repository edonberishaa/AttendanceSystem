import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import os

def get_db_connection():
    return pyodbc.connect(
        'DRIVER={ODBC Driver 17 for SQL Server};'
        'SERVER=(localdb)\\MSSQLLocalDB;'
        'DATABASE=AttendanceTest;'
        'Trusted_Connection=yes;'
    )

def fetch_attendance_status_data(conn):
    query = """
        SELECT 
            SubjectId,
            SUM(CASE WHEN Present = 1 THEN 1 ELSE 0 END) AS PresentCount,
            SUM(CASE WHEN Present = 0 THEN 1 ELSE 0 END) AS AbsentCount
        FROM Attendances
        GROUP BY SubjectId
    """
    return pd.read_sql(query, conn)

def prepare_data_for_status_plot(df):
    df_long = df.melt(
        id_vars='SubjectId', 
        value_vars=['PresentCount', 'AbsentCount'], 
        var_name='Status', 
        value_name='Count'
    )
    df_long['Status'] = df_long['Status'].map({'PresentCount': 'Present', 'AbsentCount': 'Absent'})
    return df_long

def plot_and_save_status_graph(df_long, filename="graph_attendance_status.png"):
    plt.figure(figsize=(8, 5))
    sns.barplot(data=df_long, x='SubjectId', y='Count', hue='Status')
    plt.title("Attendance Status per Subject")
    plt.xlabel("Subject ID")
    plt.ylabel("Number of Students")
    plt.legend(title="Attendance")

    save_path = save_graph(filename)
    plt.tight_layout()
    plt.savefig(save_path)
    plt.close()
    print("Status graph saved to", save_path)

def fetch_total_attendance_data(conn):
    query = """
        SELECT SubjectId, COUNT(*) AS TotalAttendance
        FROM Attendances
        GROUP BY SubjectId
    """
    return pd.read_sql(query, conn)

def plot_and_save_total_graph(df, filename="graph_total_attendance.png"):
    plt.figure(figsize=(6, 4))
    sns.barplot(data=df, x="SubjectId", y="TotalAttendance")
    plt.title("Total Attendance per Subject")
    plt.xlabel("Subject ID")
    plt.ylabel("Total Number of Attendances")

    save_path = save_graph(filename)
    plt.tight_layout()
    plt.savefig(save_path)
    plt.close()
    print("Total attendance graph saved to", save_path)

def save_graph(filename):
    graph_dir = os.path.join("wwwroot", "img")
    os.makedirs(graph_dir, exist_ok=True)
    return os.path.join(graph_dir, filename)

def main():
    conn = get_db_connection()

    # Graph 1: Present vs Absent per subject
    df_status = fetch_attendance_status_data(conn)
    df_status_long = prepare_data_for_status_plot(df_status)
    plot_and_save_status_graph(df_status_long)

    # Graph 2: Total attendance per subject
    df_total = fetch_total_attendance_data(conn)
    plot_and_save_total_graph(df_total)

if __name__ == "__main__":
    main()

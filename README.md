# 📘 Attendance System

## 📝 Project Description

The **Attendance System** is a web-based application developed to streamline and automate student attendance tracking for professors. The system integrates with an **Arduino fingerprint scanner**, enabling seamless and secure attendance verification.

**Key Features:**
- Fingerprint-based student verification via Arduino.
- Real-time updates using SignalR.
- Attendance record filtering (by date/week).
- Excel export functionality.
- Subject and student management through an intuitive dashboard.

**Tech Stack:**
- **Backend:** ASP.NET Core  
- **Database:** Entity Framework Core (SQL Server)  
- **Real-time Communication:** SignalR  
- **Hardware:** Arduino with fingerprint sensor  

---

## 👥 Team Members

| Name            | Role                                          |
|-----------------|-----------------------------------------------|
| Edon Berisha    | Full Stack Developer & Project Lead           |
| Alban Rrahmani  | Hardware Engineer, Full-Stack Developer       |
| Petrit Rexha    | Data Analyst                                  |
| Leutrim Istrefi | Node.js Developer                             |

---

## ⚙️ How to Set Up

### 🔧 Prerequisites

**Software Requirements:**
- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download) or later
- Visual Studio or VS Code
- SQL Server
- [Arduino IDE](https://www.arduino.cc/en/software) (for hardware setup)

**Hardware Requirements:**
- Arduino board with fingerprint sensor module

**Dependencies:**
Install the required NuGet packages:
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package ClosedXML
dotnet add package Microsoft.AspNetCore.SignalR
```

## 🔧 Setup Instructions (Detailed)

### 1. Clone the Repository:
```bash
git clone https://github.com/edonberishaa/AttendanceSystem.git
cd AttendanceSystem

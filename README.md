Attendance System
Project Description
The Attendance System is a web-based application designed to manage and track student attendance for professors. 
The system integrates with an Arduino device to verify student fingerprints, allowing seamless attendance recording.
Professors can view attendance records, filter them by date or week, export data to Excel, and manage subjects and students through an intuitive dashboard.
The application uses ASP.NET Core for the backend, Entity Framework Core for database management, and SignalR for real-time communication with the Arduino device.

Team Members
Edon Berisha - Full Stack Developer & Project Lead
Alban Rrahmani - Arduino Programmer,Front-End Developer
Petrit Rexha - Data Analyst
Leutrim Istrefi - nodeJs developer

How to Set Up
Prerequisites
Software Requirements :
.NET 6 SDK or later.
Visual Studio or VS Code .
SQL Server.
Arduino IDE (if working with the hardware component).
Hardware Requirements :
Arduino device with fingerprint sensor module (for fingerprint verification).
Dependencies :
Install required NuGet packages: Microsoft.EntityFrameworkCore,Microsoft.EntityFrameworkCore.Tools,Microsoft.EntityFrameworkCore.SqlServer, ClosedXML, Microsoft.AspNetCore.SignalR.

Setup Instructions
1.Clone the Repository :
git clone https://github.com/edonberishaa/AttendanceSystem.git
cd AttendanceSystem

2.Set Up the Database :
Update the connection string in appsettings.json to point to your database.
Run the following commands to apply migrations:
dotnet ef database update

3.Configure Arduino Device :
Connect the Arduino device to your computer.
Upload the provided Arduino sketch to the device using the Arduino IDE.
Ensure the COM port is correctly configured in the application settings.

4.Run the Application :
Start the application using:
dotnet run
Access the application at http://localhost:5000 or https://localhost:5001.

5.Testing the System :
Log in as a professor using valid credentials.
Use the dashboard to manage subjects, view attendance, and export data.

Project Timeline
Week 5 : Project setup and planning
Define project scope, requirements, and architecture.
Set up the development environment and database.
Week 6-7 : Implementation
Develop core features: user authentication, subject management, attendance tracking, and Arduino integration.
Implement API endpoints for CRUD operations.
Week 8 : Testing
Perform unit testing and integration testing.
Test Arduino fingerprint verification and real-time updates.
Week 9 : Deployment
Deploy the application to a hosting platform (e.g., Azure, AWS).
Configure production settings and ensure security.
Week 10 : Final Presentation
Prepare a demo showcasing all features.
Present the project and gather feedback.





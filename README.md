# KeepTrack – Task Management Application

KeepTrack is a full-stack web-based task management application developed to help users create, organise, schedule, and track their tasks.

The application provides task management functionality alongside an automated notification system that generates task reminders and overdue notifications, helping users stay informed about their scheduled tasks.

🌐 **Live Application:** [https://keeptrack.hostless.app/](https://keeptrack.hostless.app/)

---

## 🚀 Features

KeepTrack provides functionality for:

- **Task Management** – Create, edit, organise, and manage tasks.
- **Task Scheduling** – Set task start dates, due dates, and scheduling details.
- **Task Reminders** – Configure reminders for individual tasks using custom time intervals.
- **Overdue Notifications** – Generate notifications when tasks pass their due dates.
- **Browser Push Notifications** – Deliver task notifications through the browser.
- **Notification Management** – View and manage generated notifications through the application.
- **Background Processing** – Automatically generate and process notifications independently of normal user activity.

---

## 🎯 Why KeepTrack?

Managing tasks involves more than simply recording what needs to be done. Users also need a way to organise schedules, keep track of deadlines, and receive timely reminders.

KeepTrack was developed to bring these capabilities together in one web-based application.

The project also provided practical experience in:

- Full-stack application development
- Backend application architecture
- Database design and integration
- Task scheduling and reminder logic
- Background processing
- Browser push notification delivery
- External service integration
- Production deployment

---

## 🛠️ Technologies

**Backend**
- C#
- ASP.NET Core
- Razor Pages
- Entity Framework Core
- PostgreSQL

**Frontend**
- HTML5
- CSS3
- JavaScript

**Notification Processing**
- Cloudflare Workers
- Cloudflare Hyperdrive
- Browser Push Notifications
- Web Push / VAPID

**Development & Deployment**
- Git
- GitHub
- Visual Studio Code
- Hostless
- Neon PostgreSQL

---

## 🔔 Notification System

KeepTrack uses a separate notification-processing service to handle automated task notifications.

The system is responsible for two main operations:

### Notification Generation

- Checks tasks with reminders enabled.
- Determines when scheduled task reminders should be generated.
- Identifies overdue tasks.
- Creates notifications for eligible tasks.

### Notification Delivery

- Processes pending notifications.
- Delivers browser push notifications.
- Tracks notification delivery status.
- Handles notification processing independently of the main application.

The notification worker runs automatically on a scheduled basis using Cloudflare Workers.

---

## 🏗️ Application Architecture

KeepTrack uses a main web application alongside a separate notification-processing service.

### Main Application

- Handles user interactions and task management.
- Manages application data through PostgreSQL.
- Provides the interface for viewing and managing notifications.

### Notification Worker

- Runs scheduled notification-generation and delivery operations.
- Connects to the PostgreSQL database through Cloudflare Hyperdrive.
- Processes pending notifications and delivers browser push messages.

This separation allows notification processing to operate independently of the main web application.

---

## 💻 Running the Project Locally

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- Git

### 1. Clone the Repository

```bash
git clone https://github.com/smangajasithole-developer/KeepTrack.git
```

Navigate into the project:

```bash
cd KeepTrack
```

### 2. Configure the Database

Create a PostgreSQL database and configure the required database connection string using the application's development configuration.

### 3. Configure Application Secrets

Configure the required application settings and secrets for your local environment. These may include database connection details and notification-related configuration.

Never commit passwords, connection strings, private keys, or other sensitive credentials to GitHub.

### 4. Restore Dependencies

```bash
dotnet restore
```

### 5. Apply Database Migrations

```bash
dotnet ef database update --context PostgresKeepTrackDbContext -- --environment Development
```

### 6. Run the Application

```bash
dotnet run
```

Open the local address displayed in the terminal to access the application.

---

## 📦 Notification Worker

The notification-processing service is maintained separately from the main application.

It is built using Cloudflare Workers and uses Cloudflare Hyperdrive to connect to the PostgreSQL database.

Worker deployment and configuration are managed separately from the ASP.NET Core application.

The notification worker source code is maintained in a separate repository:

[KeepTrack Notifications Worker](https://github.com/smangajasithole-developer/KeepTrack-Notifications-Worker)

---

## ☁️ Production Deployment

The production application uses:

- **Application Hosting:** Hostless
- **Database:** Neon PostgreSQL
- **Notification Processing:** Cloudflare Workers
- **Database Connectivity for Worker:** Cloudflare Hyperdrive

The application and notification worker are deployed independently.

Production configuration and sensitive environment variables are managed separately from the source code.

---

## 🔄 Development Approach

KeepTrack was developed incrementally, with attention to application functionality, database integration, and reliable notification processing.

Development areas included:

- Designing the application structure
- Implementing task management functionality
- Developing task scheduling and reminder logic
- Integrating PostgreSQL using Entity Framework Core
- Implementing overdue notification generation
- Developing a separate notification-processing worker
- Integrating browser push notifications
- Configuring production hosting and database connectivity
- Testing notification generation and delivery
- Deploying the application and notification worker

---

## 👨‍💻 Developer

**Smanga James Sithole**

Diploma in Information and Communication Technology (ICT)

KeepTrack was developed as a practical full-stack software project focused on task management, automated notifications, background processing, and production deployment.

---

## 📄 License

This project currently does not include an open-source license.

All rights are reserved unless otherwise stated.

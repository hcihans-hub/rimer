# 🏗️ RIMER Project Architecture and Execution Guide

This document explains the technical structure, layered architecture, and how to set up the Rectorate Communication Center (RIMER) project.

---

## 📌 Project Overview
RIMER is a high-performance, secure ticket management system designed to digitalize communication between university management and stakeholders (students, staff, and external users).

---

## 🏛️ Technical Architecture (Clean Architecture)

The project is structured according to **Clean Architecture** principles, where dependencies flow inward and core business logic (Domain) remains at the center:

### 1. RimerApi.Domain
- **Purpose:** The heart of the system. It has no dependencies on external libraries or other layers.
- **Content:** Core entities, Value Objects, Enums, and Domain rules.

### 2. RimerApi.Application
- **Purpose:** Manages business logic and use cases.
- **Content:** Service interfaces, DTOs, AutoMapper profiles, and validation rules.

### 3. RimerApi.Infrastructure
- **Purpose:** Handles the system's interaction with the external world.
- **Content:** 
  - **Data:** Entity Framework Core (SQL Server) and Migrations.
  - **Identity:** JWT-based authentication and authorization services.
  - **WebSockets:** Real-time notification hubs using SignalR.
  - **Background Jobs:** Asynchronous processing (e.g., Audit log archiving) via Hangfire.

### 4. RimerApi.API
- **Purpose:** The entry point of the system.
- **Content:** RESTful Controllers, Swagger documentation, Middlewares, and configuration.

---

## 🛠️ Technologies Used

### Backend
- **ASP.NET Core 10:** Modern and high-speed web API framework.
- **EF Core:** Database management layer.
- **SignalR:** Real-time bi-directional communication.
- **Hangfire:** Background job management and scheduled tasks.
- **Serilog:** Structured logging.
- **OpenTelemetry:** Metrics and observability.

### Frontend
- **Vue.js 3:** Reactive and modern UI framework.
- **Vite:** Fast development and build tool.
- **Tailwind CSS:** Utility-first styling library.
- **ECharts:** Data visualization and analysis charts.

---

## ⚙️ Execution Steps

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js (v20+)](https://nodejs.org/)
- SQL Server (Or SQL Express via Docker)

### 1. Backend Setup
Navigate to the project root directory in your terminal:

```powershell
# Go to the API directory
cd src/RimerApi.API

# Install dependencies and run the project
dotnet run
```
The API serves at `http://localhost:5000` by default. You can access the Swagger UI at `http://localhost:5000/swagger`.

### 2. Frontend Setup
Open a new terminal and navigate to the dashboard directory:

```powershell
# Go to the Dashboard directory
cd rimer-dashboard

# Install dependencies
npm install

# Start the development server
npm run dev
```
The application will be accessible at `http://localhost:5173/`.

---

## 🛡️ System Protection and Security
- **Adaptive Load Shedding:** Automatically mitigates load when CPU or RAM usage reaches critical levels.
- **JWT Auth:** Secure authentication and role-based authorization (Admin, Staff, Student).
- **Audit Logging:** All critical actions are logged asynchronously and archived periodically.

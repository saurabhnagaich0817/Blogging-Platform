# 🖋️ InkWell - Microservices Blogging Platform (Backend)

InkWell is a high-performance, scalable blogging platform built using a **Microservices Architecture** with **.NET 8**. It leverages modern cloud-native patterns like Event-Driven Communication, API Gateway, and Distributed Logging.

---

## 🏗️ Architecture Overview

The system is divided into specialized microservices that communicate asynchronously using **RabbitMQ** (via MassTransit).

### 🛠️ Tech Stack
- **Framework**: .NET 8 (C#)
- **Gateway**: Microsoft YARP (Reverse Proxy)
- **Messaging**: RabbitMQ + MassTransit
- **Database**: SQL Server (Entity Framework Core)
- **Authentication**: JWT (JSON Web Token) + Google OAuth
- **Logging**: Serilog (File + Console)
- **Containerization**: Docker & Docker Compose

---

## 📂 Microservices Breakdown

1. **API Gateway**: Single entry point. Handles routing, CORS, and centralized Auth validation.
2. **Auth Service**: Manages User Identity, Roles (Admin/Reader), and Google Login.
3. **Post Service**: Core logic for Story creation, editing, analytics, and Likes.
4. **Category Service**: Manages taxonomy (Technology, Health, etc.) with automated seeding.
5. **Comment Service**: Real-time interaction logic for reader engagement.
6. **Media Service**: Handles image uploads and static content serving.
7. **Newsletter Service**: Manages subscribers and triggers email broadcasts.
8. **Notification Service**: Centralized hub for sending Emails and System Alerts.

---

## 🚀 Local Setup (Development)

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or SSMS)
- RabbitMQ (Running on localhost:5672)

### Steps
1. **Clone the Repo**:
   ```bash
   git clone <your-backend-repo-url>
   ```
2. **Setup Database**:
   Update `appsettings.json` in each service with your SQL connection string.
3. **Run Services**:
   Open each service folder and run:
   ```bash
   dotnet run
   ```
   *Tip: Run the Gateway last (localhost:5000).*

---

## 🐳 Docker Deployment

The project is fully containerized. To run the entire ecosystem (including DB and RabbitMQ):
```bash
docker-compose up --build
```

---

## 👔 Interview Talking Points
- **Scalability**: Each service can be scaled independently based on traffic.
- **Resilience**: Newsletter failure doesn't affect the Post Service.
- **Decoupling**: Services communicate via Events (Publish/Subscribe) instead of direct HTTP calls.

---

Developed with ❤️ by **Saurabh Nagayach**

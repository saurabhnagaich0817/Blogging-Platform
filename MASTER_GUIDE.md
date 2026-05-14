# InkWell Platform - Master Execution Guide

Welcome to the InkWell Platform. This guide contains all the necessary commands to run, test, and analyze the microservices-based blogging platform.

---

## 1. Infrastructure Setup (Prerequisites)
Ensure Docker Desktop is running before executing these.

### RabbitMQ & PostgreSQL
Starts the message broker and databases in the background.
```powershell
docker-compose up -d
```

### SonarQube (Quality Audit)
Starts the SonarQube container for code quality analysis.
```powershell
docker-compose -f docker-compose.sonarqube.yml up -d
```

---

## 2. Backend Microservices (.NET 8)

### Build All Services
```powershell
dotnet build
```

### Run All Services (Master Script)
If you have a run script:
```powershell
./run-all.ps1
```
*Manual Start:* Run `dotnet run` in each service folder under `Backend/src/`.

### Run Backend Unit Tests
```powershell
dotnet test
```

---

## 3. Frontend Application (Angular 17)
Navigate to the frontend directory first: `cd Frontend/inkwell-frontend`

### Start Development Server
```powershell
npm start
```
Access at: `http://localhost:4200`

### Run Frontend Unit Tests (Karma)
```powershell
npm run test
```

### Run End-to-End Tests (Playwright)
```powershell
npm run test:e2e
```

---

## 4. Code Quality & Analysis (SonarQube)
Run the master analysis script from the root directory to scan both Backend and Frontend.
```powershell
./analyze-all.ps1
```
View results at: `http://localhost:9000`

---

## 5. API Gateway
All requests should go through the YARP Gateway.
**Gateway URL:** `https://localhost:7001` (or the configured port).

---

## Troubleshooting
- **502 Bad Gateway:** Wait 10-15 seconds for microservices to fully initialize.
- **RabbitMQ Connection Error:** Ensure Docker container `inkwell-rabbitmq` is "Healthy".
- **Playwright Missing Module:** Run `npm install @playwright/test --save-dev`.

---
*Created for Capgemini Mentorship Program - InkWell Platform.*

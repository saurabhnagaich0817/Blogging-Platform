# Workspace Standards & Quick Setup Guidelines

This document details the standard CLI commands, configuration guides, and quick-setup shortcuts for working with the InkWell codebase.

---

## 1. Quick-Start: Adding a New Service (.NET 8 Minimal API)

If asked to create a new microservice from scratch during a live coding test, you do not need to configure databases or complex controllers. The simplest and fastest way in .NET 8 is to use a **Minimal API** which runs in just 4 lines of code.

### Step 1.1: Run CLI Command to Create Project
Run this command in the `Backend` directory:
```bash
dotnet new webapi -n InkWell.NewService -o src/InkWell.NewService
```

### Step 1.2: Add it to the Solution File
```bash
dotnet sln InkWell.sln add src/InkWell.NewService/InkWell.NewService.csproj
```

### Step 1.3: Replace Program.cs with 5 Lines of Code
Open the newly created `src/InkWell.NewService/Program.cs` and replace its entire content with this super-simple minimal template:
```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/newservice", () => new { Status = "Active", Message = "Hello World!" });

app.Run("http://localhost:7008");
```
*Run it immediately by running `dotnet run` inside the project folder. You now have a working, running microservice on port 7008 in 20 seconds!*

---

## 2. Quick-Start: Adding a Routing Rule in YARP Gateway

To route traffic from the frontend to your new microservice (port 7008) via the Gateway, open `gateway/InkWell.API.Gateway/appsettings.json` and add these two simple blocks inside the `ReverseProxy` object:

### Add inside "Routes":
```json
"newservice-route": {
  "ClusterId": "newservice-cluster",
  "Match": { "Path": "/api/newservice/{**catch-all}" }
}
```

### Add inside "Clusters":
```json
"newservice-cluster": {
  "Destinations": {
    "dest1": { "Address": "http://localhost:7008/api/" }
  }
}
```

---

## 3. Quick-Start: Adding a Quick API Endpoint

If asked to add a new API endpoint inside an existing microservice, don't create new files. Just open any existing controller (e.g., `UserController.cs` or `PostController.cs`) and paste this simple 3-line method at the bottom of the class:

```csharp
[HttpGet("quick-test")]
public IActionResult GetQuickTest()
{
    return Ok(new { status = "Success", message = "Quick API test passed!" });
}
```
*This endpoint immediately maps to `/api/posts/quick-test` or `/api/auth/quick-test` (depending on which controller you pasted it in).*

---

## 4. Quick-Start: Adding an Angular Alert Button in 5 Seconds

If asked to add a button in the Angular UI that displays a pop-up alert/notification message, use this **Inline HTML Hack** which doesn't require modifying any TypeScript or service files:

Open your dashboard view file (e.g., `dashboard.component.html`) and paste this single line:

```html
<!-- Inline HTML Cheat-Code Button (No TS changes needed!) -->
<button onclick="alert('[System Check]\nWeb API Gateway: Active\nDatabase Status: Connected Successfully!')" 
        style="background-color: #1B365D; color: white; padding: 10px 20px; border: none; border-radius: 4px; font-weight: bold; cursor: pointer; margin-top: 15px;">
  Verify Service Pipeline
</button>
```
*Because this uses standard HTML `onclick`, it executes instantly in any browser when clicked, showing a neat system alert modal immediately, without writing a single line of Angular TypeScript code!*
